using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OzarkLMS.Data;
using OzarkLMS.Models;
using System.Security.Claims;

namespace OzarkLMS.Controllers
{
    [Authorize]
    public class CollaborationController : Controller
    {
        private readonly AppDbContext _context;

        public CollaborationController(AppDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("UserId");
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
        }

        // GET: /Collaboration
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return RedirectToAction("Login", "Account");

            // Ensure membership in Default Chat ("Main Organization Chat")
            var defaultChats = await _context.ChatGroups.Where(g => g.IsDefault).ToListAsync();
            foreach (var defaultChat in defaultChats)
            {
                var isMember = await _context.ChatGroupMembers
                    .AnyAsync(m => m.ChatGroupId == defaultChat.Id && m.UserId == userId);

                if (!isMember)
                {
                    _context.ChatGroupMembers.Add(new ChatGroupMember
                    {
                        ChatGroupId = defaultChat.Id,
                        UserId = userId,
                        JoinedDate = DateTime.UtcNow
                    });
                }
            }
            if (_context.ChangeTracker.HasChanges()) await _context.SaveChangesAsync();

            // Get user's groups (Member of)
            var myGroups = await _context.ChatGroupMembers
                .Where(m => m.UserId == userId)
                .Include(m => m.ChatGroup)
                    .ThenInclude(g => g.CreatedBy)
                .Include(m => m.ChatGroup)
                    .ThenInclude(g => g.Messages)
                .Select(m => m.ChatGroup)
                .OrderByDescending(g => g.IsDefault) // Default first
                .ThenByDescending(g => g.LastActivityDate) // Then by last activity
                .ToListAsync();

            // Admins can see all groups? The requirement says "Admin accounts have visibility into all group chats"
            // But usually "Index" shows "My Chats". If Admin wants to manage, maybe a standard list?
            // "Each user’s profile must automatically display a list of all group chats they are currently a member of"
            // Let's stick to "My Groups" for the main view, but if Admin, maybe show all (or have a toggle). 
            // For now, let's just show My Groups + Default (which includes Admin).
            // If Admin needs to see others, they might need a special Admin View. 
            // However, requirement 2: "Admin accounts have visibility into all group chats".
            // Let's append ALL other chats if User is Admin, distinct.

            var privateChats = await _context.PrivateChats
                .Include(c => c.User1)
                .Include(c => c.User2)
                .Where(c => c.User1Id == userId || c.User2Id == userId)
                .OrderByDescending(c => c.LastActivityDate)
                .ToListAsync();

            if (User.IsInRole("admin"))
            {
                var allGroups = await _context.ChatGroups
                    .Include(g => g.CreatedBy)
                    .Include(g => g.Messages)
                    .OrderByDescending(g => g.IsDefault)
                    .ThenByDescending(g => g.LastActivityDate)
                    .ToListAsync();

                ViewBag.PrivateChats = privateChats;
                return View(allGroups);
            }

            ViewBag.PrivateChats = privateChats;
            return View(myGroups);
        }

        // POST: /Collaboration/CreateGroup
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateGroup(string name, string description, IFormFile? photo)
        {
            if (string.IsNullOrWhiteSpace(name)) return RedirectToAction(nameof(Index));

            var userId = GetCurrentUserId();
            string? photoUrl = null;

            if (photo != null && photo.Length > 0)
            {
                var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(photo.FileName);
                await using (var stream = new FileStream(Path.Combine(uploads, fileName), FileMode.Create))
                {
                    await photo.CopyToAsync(stream);
                }
                photoUrl = "/uploads/" + fileName;
            }

            var group = new ChatGroup
            {
                Name = name,
                Description = description ?? "",
                CreatedById = userId,
                OwnerId = userId, // Creator is owner
                IsDefault = false,
                GroupPhotoUrl = photoUrl
            };

            _context.ChatGroups.Add(group);
            await _context.SaveChangesAsync();

            // Creator is automatically a member
            _context.ChatGroupMembers.Add(new ChatGroupMember
            {
                ChatGroupId = group.Id,
                UserId = userId,
                JoinedDate = DateTime.UtcNow,
                ViewMode = "List"
            });
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = group.Id });
        }

        // GET: /Collaboration/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var userId = GetCurrentUserId();
            var isAdmin = User.IsInRole("admin");

            var group = await _context.ChatGroups
                .Include(g => g.Messages)
                    .ThenInclude(m => m.Sender)
                .Include(g => g.Members)
                    .ThenInclude(m => m.User) // Include User info for members list
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null) return NotFound();

            // Check Visibility
            var isMember = group.Members.Any(m => m.UserId == userId);
            if (!isMember)
            {
                // Requirement: "Group chats are visible only to users who are members" (EXCEPT Admin)
                if (!isAdmin)
                {
                    // If it's a default chat and they aren't a member (rare race condition or bug), fix it
                    if (group.IsDefault)
                    {
                        _context.ChatGroupMembers.Add(new ChatGroupMember { ChatGroupId = group.Id, UserId = userId, JoinedDate = DateTime.UtcNow });
                        await _context.SaveChangesAsync();
                        isMember = true;
                    }
                    else
                    {
                        return Forbid();
                    }
                }
            }

            return View(group);
        }

        // POST: /Collaboration/PostMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PostMessage(int groupId, string? message, IFormFile? file)
        {
            if (string.IsNullOrWhiteSpace(message) && file == null) return RedirectToAction(nameof(Details), new { id = groupId });

            var userId = GetCurrentUserId();
            // Validate membership/access again
            var group = await _context.ChatGroups.FindAsync(groupId);
            if (group == null) return NotFound();

            var isMember = await _context.ChatGroupMembers.AnyAsync(m => m.ChatGroupId == groupId && m.UserId == userId);
            if (!isMember && !User.IsInRole("admin")) return Forbid();

            string? attachmentUrl = null;
            string? originalName = null;
            string? contentType = null;
            long size = 0;

            if (file != null && file.Length > 0)
            {
                var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(uploads, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                attachmentUrl = "/uploads/" + fileName;
                originalName = file.FileName;
                contentType = file.ContentType;
                size = file.Length;
            }

            var chatMessage = new ChatMessage
            {
                GroupId = groupId,
                SenderId = userId,
                Message = message ?? (file != null ? "" : ""), // Allowing empty message if file exists
                AttachmentUrl = attachmentUrl,
                AttachmentOriginalName = originalName,
                AttachmentContentType = contentType,
                AttachmentSize = size,
                SentDate = DateTime.UtcNow
            };

            _context.ChatMessages.Add(chatMessage);

            // Update LastActivityDate
            group.LastActivityDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Notify Group Members (excluding sender)
            var otherMembers = await _context.ChatGroupMembers
                .Where(m => m.ChatGroupId == groupId && m.UserId != userId)
                .Select(m => m.UserId)
                .ToListAsync();

            var notifications = otherMembers.Select(memberId => new Notification
            {
                RecipientId = memberId,
                SenderId = userId,
                Title = $"New Message in {group.Name}",
                Message = message?.Length > 50 ? message.Substring(0, 47) + "..." : (string.IsNullOrWhiteSpace(message) ? "Sent an attachment" : message),
                SentDate = DateTime.UtcNow,
                IsRead = false,
                ActionUrl = $"/Collaboration/Details/{groupId}"
            });

            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = groupId });
        }

        // POST: /Collaboration/EditMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMessage(int messageId, string newContent)
        {
            var userId = GetCurrentUserId();
            var message = await _context.ChatMessages.Include(m => m.Group).FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null) return NotFound();

            // Only sender can edit
            if (message.SenderId != userId) return Forbid();

            // Perform Edit
            message.Message = newContent;
            message.LastEditedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = message.GroupId });
        }

        // POST: /Collaboration/DeleteMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMessage(int messageId)
        {
            var userId = GetCurrentUserId();
            var message = await _context.ChatMessages.Include(m => m.Group).FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null) return NotFound();

            // Sender can delete. Admin maybe? Requirement says "Delete their own messages". 
            // Let's allow Admin too just in case, but primary is sender.
            bool isAdmin = User.IsInRole("admin");
            if (message.SenderId != userId && !isAdmin) return Forbid();

            // Soft Delete
            message.IsDeleted = true;
            // Clear content for compliance/safety (optional but good practice for soft delete if we want to hide it completely)
            message.Message = "";
            message.AttachmentUrl = null;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = message.GroupId });
        }

        // POST: /Collaboration/StartPrivateChat
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartPrivateChat(string targetUsername)
        {
            var userId = GetCurrentUserId();
            var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == targetUsername);

            if (targetUser == null) return RedirectToAction(nameof(Index));
            if (targetUser.Id == userId) return RedirectToAction(nameof(Index)); // Cannot chat with self

            // Check for existing chat
            var existingChat = await _context.PrivateChats
                .FirstOrDefaultAsync(c => (c.User1Id == userId && c.User2Id == targetUser.Id) ||
                                          (c.User1Id == targetUser.Id && c.User2Id == userId));

            if (existingChat != null)
            {
                return RedirectToAction("PrivateDetails", new { id = existingChat.Id });
            }

            // Create new Private Chat
            var newChat = new PrivateChat
            {
                User1Id = userId,
                User2Id = targetUser.Id,
                LastActivityDate = DateTime.UtcNow
            };

            _context.PrivateChats.Add(newChat);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(PrivateDetails), new { id = newChat.Id });
        }

        // GET: /Collaboration/PrivateDetails/5
        public async Task<IActionResult> PrivateDetails(int id)
        {
            var userId = GetCurrentUserId();
            var chat = await _context.PrivateChats
                .Include(c => c.User1)
                .Include(c => c.User2)
                .Include(c => c.Messages)
                    .ThenInclude(m => m.Sender)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (chat == null) return NotFound();

            // Security Check
            if (chat.User1Id != userId && chat.User2Id != userId && !User.IsInRole("admin"))
            {
                return Forbid();
            }

            return View(chat);
        }

        // POST: /Collaboration/PostPrivateMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PostPrivateMessage(int chatId, string? message, IFormFile? file)
        {
            if (string.IsNullOrWhiteSpace(message) && file == null) return RedirectToAction(nameof(PrivateDetails), new { id = chatId });

            var userId = GetCurrentUserId();
            var chat = await _context.PrivateChats.FindAsync(chatId);
            if (chat == null) return NotFound();

            if (chat.User1Id != userId && chat.User2Id != userId && !User.IsInRole("admin")) return Forbid();

            string? attachmentUrl = null;
            string? originalName = null;
            string? contentType = null;
            long size = 0;

            if (file != null && file.Length > 0)
            {
                var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(uploads, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                attachmentUrl = "/uploads/" + fileName;
                originalName = file.FileName;
                contentType = file.ContentType;
                size = file.Length;
            }

            var privateMessage = new PrivateMessage
            {
                PrivateChatId = chatId,
                SenderId = userId,
                Message = message ?? (file != null ? "" : ""),
                AttachmentUrl = attachmentUrl,
                AttachmentOriginalName = originalName,
                AttachmentContentType = contentType,
                AttachmentSize = size,
                SentDate = DateTime.UtcNow
            };

            _context.PrivateMessages.Add(privateMessage);
            chat.LastActivityDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Notify Recipient
            var recipientId = chat.User1Id == userId ? chat.User2Id : chat.User1Id;
            var sender = await _context.Users.FindAsync(userId);
            var senderName = sender?.Username ?? "Someone";

            _context.Notifications.Add(new Notification
            {
                RecipientId = recipientId,
                SenderId = userId,
                Title = $"Private Message from {senderName}",
                Message = message?.Length > 50 ? message.Substring(0, 47) + "..." : (string.IsNullOrWhiteSpace(message) ? "Sent an attachment" : message),
                SentDate = DateTime.UtcNow,
                IsRead = false,
                ActionUrl = $"/Collaboration/PrivateDetails/{chatId}"
            });
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(PrivateDetails), new { id = chatId });
        }

        // POST: /Collaboration/DeletePrivateMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePrivateMessage(int messageId)
        {
            var userId = GetCurrentUserId();
            var message = await _context.PrivateMessages.Include(m => m.Chat).FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null) return NotFound();
            if (message.SenderId != userId && !User.IsInRole("admin")) return Forbid();

            message.IsDeleted = true;
            message.Message = "";
            message.AttachmentUrl = null;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(PrivateDetails), new { id = message.Chat.Id });
        }

        // POST: /Collaboration/EditPrivateMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPrivateMessage(int messageId, string newContent)
        {
            var userId = GetCurrentUserId();
            var message = await _context.PrivateMessages.Include(m => m.Chat).FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null) return NotFound();
            if (message.SenderId != userId) return Forbid();

            message.Message = newContent;
            message.LastEditedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(PrivateDetails), new { id = message.Chat.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleViewMode(int groupId, string viewMode)
        {
            var userId = GetCurrentUserId();
            var member = await _context.ChatGroupMembers.FirstOrDefaultAsync(m => m.ChatGroupId == groupId && m.UserId == userId);

            if (member != null)
            {
                member.ViewMode = viewMode;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = groupId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateGroupPhoto(int groupId, IFormFile? photo, string? photoUrl)
        {
            var group = await _context.ChatGroups.FindAsync(groupId);
            if (group == null) return NotFound();

            var currentUserId = GetCurrentUserId();
            bool isOwner = group.OwnerId == currentUserId;
            bool isAdmin = User.IsInRole("admin");

            if (!isOwner && !isAdmin) return Forbid();

            if (photo != null && photo.Length > 0)
            {
                var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(photo.FileName);
                await using (var stream = new FileStream(Path.Combine(uploads, fileName), FileMode.Create))
                {
                    await photo.CopyToAsync(stream);
                }
                group.GroupPhotoUrl = "/uploads/" + fileName;
            }
            else if (!string.IsNullOrEmpty(photoUrl))
            {
                group.GroupPhotoUrl = photoUrl;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = groupId });
        }

        // POST: /Collaboration/AddMember
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMember(int groupId, string username)
        {
            var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (targetUser == null) return RedirectToAction(nameof(Details), new { id = groupId });

            var group = await _context.ChatGroups.Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == groupId);
            if (group == null) return NotFound();

            var currentUserId = GetCurrentUserId();

            // Check permissions: Creator/Owner can add members.
            // Requirement 2: "The creator can add or remove members" (We interpreted Creator as Owner here)
            // Also Admin? It says "Only admin accounts can manage or remove [the default chat]". 
            // For custom chats, Owner can manage.
            bool isOwner = group.OwnerId == currentUserId;
            bool isAdmin = User.IsInRole("admin");

            if (group.IsDefault)
            {
                // Only Admin can manage Default Chat
                if (!isAdmin) return Forbid();
            }
            else
            {
                // Only Owner or Admin can manage Custom Chat
                if (!isOwner && !isAdmin) return Forbid();
            }

            if (!group.Members.Any(m => m.UserId == targetUser.Id))
            {
                _context.ChatGroupMembers.Add(new ChatGroupMember
                {
                    ChatGroupId = groupId,
                    UserId = targetUser.Id
                });

                // Notification
                var senderName = User.Identity!.Name;
                _context.Notifications.Add(new Notification
                {
                    RecipientId = targetUser.Id,
                    Title = "Added to Group",
                    Message = $"{senderName} added you to '{group.Name}'",
                    SenderId = currentUserId
                });

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = groupId });
        }

        // POST: /Collaboration/RemoveMember
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMember(int groupId, int memberId)
        {
            var group = await _context.ChatGroups.Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == groupId);
            if (group == null) return NotFound();

            var currentUserId = GetCurrentUserId();
            bool isOwner = group.OwnerId == currentUserId;
            bool isAdmin = User.IsInRole("admin");

            if (group.IsDefault)
            {
                if (!isAdmin) return Forbid();
            }
            else
            {
                if (!isOwner && !isAdmin) return Forbid();
            }

            // Cannot remove Owner (Owner must leave or transfer ownership first, or delete group)
            if (memberId == group.OwnerId)
            {
                // Maybe show error "Owner cannot be removed"?
                return RedirectToAction(nameof(Details), new { id = groupId });
            }

            var member = group.Members.FirstOrDefault(m => m.UserId == memberId);
            if (member != null)
            {
                _context.ChatGroupMembers.Remove(member);
                // Notification?
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = groupId });
        }

        // POST: /Collaboration/LeaveGroup
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LeaveGroup(int groupId)
        {
            var userId = GetCurrentUserId();
            var group = await _context.ChatGroups.Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == groupId);
            if (group == null) return NotFound();

            // Requirement 1: "Cannot be deleted or left" (for Default Chat)
            if (group.IsDefault)
            {
                return BadRequest("Cannot leave the default organization chat.");
            }

            var membership = group.Members.FirstOrDefault(m => m.UserId == userId);
            if (membership == null) return RedirectToAction(nameof(Index)); // Already not a member

            _context.ChatGroupMembers.Remove(membership);
            await _context.SaveChangesAsync();

            // Ownership Transfer Logic
            // Requirement 4: "If the creator [Owner] of a group chat leaves... ownership is automatically transferred"
            if (group.OwnerId == userId)
            {
                // Get remaining members
                // Re-fetch members to be safe since we just removed one? 
                // Tracking should handle it, but let's look at the in-memory list which now excludes the removed one (if using EF correctly)
                // Actually `membership` was removed from context, but `group.Members` list might still have it depending on tracking.
                // Safest to query DB for remaining members

                var remainingMembers = await _context.ChatGroupMembers
                    .Where(m => m.ChatGroupId == groupId && m.UserId != userId)
                    .OrderBy(m => m.JoinedDate) // "based on join order"
                    .ToListAsync();

                if (remainingMembers.Any())
                {
                    // Transfer to earliest added member
                    group.OwnerId = remainingMembers.First().UserId;
                }
                else
                {
                    // No members left. Delete group? 
                    // Requirement doesn't explicitly say to delete, but empty groups are useless.
                    // "A group chat can be deleted only by..." -> implied manual. 
                    // But if no one is left, it's orphaned. Let's delete it to keep DB clean.
                    _context.ChatGroups.Remove(group);
                }
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Collaboration/DeleteGroup
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGroup(int groupId)
        {
            var group = await _context.ChatGroups.FindAsync(groupId);
            if (group == null) return NotFound();

            var currentUserId = GetCurrentUserId();
            bool isOwner = group.OwnerId == currentUserId;
            bool isAdmin = User.IsInRole("admin");

            // Requirement 3: "A group chat can be deleted only by The account that created it [Owner], or An admin account"
            // Also Requirement 1: Default chat "Cannot be deleted" (except maybe by Admin? "Only admin accounts can manage or remove it")

            if (group.IsDefault)
            {
                // Requirement: Remove the ability for admin accounts to delete default chats
                return BadRequest("The default organization chat cannot be deleted.");
            }
            else
            {
                if (!isOwner && !isAdmin) return Forbid();
            }

            _context.ChatGroups.Remove(group);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        // GET: /Collaboration/SearchUsers
        [HttpGet]
        public async Task<IActionResult> SearchUsers(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return Json(new List<object>());

            var users = await _context.Users
                .Where(u => u.Username.ToLower().Contains(query.ToLower()))
                .Select(u => new
                {
                    id = u.Id,
                    username = u.Username,
                    profilePictureUrl = u.ProfilePictureUrl
                })
                .Take(10)
                .ToListAsync();

            return Json(users);
        }

        // GET: /Collaboration/SearchGlobal
        [HttpGet]
        public async Task<IActionResult> SearchGlobal(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return Json(new List<object>());

            var userId = GetCurrentUserId();

            // Search Users (exclude self)
            var users = await _context.Users
                .Where(u => u.Id != userId && u.Username.ToLower().Contains(query.ToLower()))
                .Select(u => new
                {
                    type = "user",
                    id = u.Id,
                    name = u.Username,
                    photo = u.ProfilePictureUrl
                })
                .Take(5)
                .ToListAsync();

            var groups = await _context.ChatGroups
                .Where(g => g.Name.ToLower().Contains(query.ToLower()))
                .Select(g => new
                {
                    type = "group",
                    id = g.Id,
                    name = g.Name,
                    photo = g.GroupPhotoUrl
                })
                .Take(5)
                .ToListAsync();

            // Merge users and groups
            var results = new List<object>();
            results.AddRange(users);
            results.AddRange(groups);

            return Json(results);
        }
    }
}
