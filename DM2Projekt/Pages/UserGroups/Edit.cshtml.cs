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
    public int OldGroupId { get; set; } // this holds the group the user was in before

    public SelectList GroupOptions { get; set; } = default!;
    public User CurrentUser { get; set; } = default!;
    public Group CurrentGroup { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? userId, int? groupId)
    {
        if (userId == null || groupId == null)
            return NotFound();

        var role = HttpContext.Session.GetString("UserRole");
        if (role != "Admin")
            return RedirectToPage("/UserGroups/Index");

        // grab the current user-group combo
        UserGroup = await _context.UserGroup
            .Include(ug => ug.User)
            .Include(ug => ug.Group)
            .FirstOrDefaultAsync(m => m.UserId == userId && m.GroupId == groupId);

        if (UserGroup == null)
            return NotFound();

        // store the old group ID so to remove it later
        OldGroupId = UserGroup.GroupId;

        // get info to show on the page
        CurrentUser = UserGroup.User;
        CurrentGroup = UserGroup.Group;

        // list of all groups for dropdown
        GroupOptions = new SelectList(_context.Group
            .OrderBy(g => g.GroupName), "GroupId", "GroupName", UserGroup.GroupId);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var role = HttpContext.Session.GetString("UserRole");
        if (role != "Admin")
            return RedirectToPage("/UserGroups/Index");

        if (!ModelState.IsValid)
        {
            // reload group list if something goes wrong
            GroupOptions = new SelectList(_context.Group.OrderBy(g => g.GroupName), "GroupId", "GroupName", UserGroup.GroupId);
            return Page();
        }

        // remove the old user-group record (the one being changed)
        var old = await _context.UserGroup.FindAsync(UserGroup.UserId, OldGroupId);
        if (old != null)
            _context.UserGroup.Remove(old);

        // add the new one
        _context.UserGroup.Add(UserGroup);
        await _context.SaveChangesAsync();

        // let user know it worked
        TempData["Success"] = "User was successfully moved to a new group.";
        return RedirectToPage("./Index");
    }
}
