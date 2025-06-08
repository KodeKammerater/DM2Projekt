using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DM2Projekt.Data;
using DM2Projekt.Models;

namespace DM2Projekt.Pages.UserGroups;

public class EditModel : PageModel
{
    private readonly DM2ProjektContext _context;

    public EditModel(DM2ProjektContext context)
    {
        _context = context;
    }

    [BindProperty]
    public UserGroup UserGroup { get; set; } = default!;

    [BindProperty]
    public int OldGroupId { get; set; }

    public SelectList GroupOptions { get; set; } = default!;
    public User CurrentUser { get; set; } = default!;
    public Group CurrentGroup { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? userId, int? groupId)
    {
        if (userId == null || groupId == null)
            return NotFound();

        // only admins allowed to edit this stuff
        var role = HttpContext.Session.GetString("UserRole");
        if (role != "Admin")
            return RedirectToPage("/UserGroups/Index");

        // grab the current user+group combo
        UserGroup = await _context.UserGroup
            .Include(ug => ug.User)
            .Include(ug => ug.Group)
            .FirstOrDefaultAsync(m => m.UserId == userId && m.GroupId == groupId);

        if (UserGroup == null)
            return NotFound();

        OldGroupId = UserGroup.GroupId;
        CurrentUser = UserGroup.User;
        CurrentGroup = UserGroup.Group;

        // for the dropdown
        GroupOptions = new SelectList(_context.Group
            .OrderBy(g => g.GroupName), "GroupId", "GroupName", UserGroup.GroupId);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // again, only admins can mess with this
        var role = HttpContext.Session.GetString("UserRole");
        if (role != "Admin")
            return RedirectToPage("/UserGroups/Index");

        if (!ModelState.IsValid)
        {
            GroupOptions = new SelectList(_context.Group.OrderBy(g => g.GroupName), "GroupId", "GroupName", UserGroup.GroupId);
            return Page();
        }

        // find the old group (where user is coming from)
        var oldGroup = await _context.Group.FindAsync(OldGroupId);

        // if user is the one who made that group...
        if (oldGroup != null && oldGroup.CreatedByUserId == UserGroup.UserId)
        {
            // ...then nuke the whole group
            _context.Group.Remove(oldGroup);

            // also clean up everyone else in it
            var oldGroupMemberships = _context.UserGroup.Where(ug => ug.GroupId == OldGroupId);
            _context.UserGroup.RemoveRange(oldGroupMemberships);
        }
        else
        {
            // just a normal member? just remove the old link
            var old = await _context.UserGroup.FindAsync(UserGroup.UserId, OldGroupId);
            if (old != null)
                _context.UserGroup.Remove(old);
        }

        // add the new group assignment
        _context.UserGroup.Add(UserGroup);
        await _context.SaveChangesAsync();

        // all done, high five
        TempData["Success"] = "User was successfully moved to a new group.";
        return RedirectToPage("./Index");
    }
}
