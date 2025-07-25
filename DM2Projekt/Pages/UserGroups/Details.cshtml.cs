﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DM2Projekt.Data;
using DM2Projekt.Models;

namespace DM2Projekt.Pages.UserGroups;

public class DetailsModel : PageModel
{
    private readonly DM2ProjektContext _context;

    public DetailsModel(DM2ProjektContext context)
    {
        _context = context;
    }

    public UserGroup UserGroup { get; set; } = new();

    public int GroupMemberCount { get; set; }
    public string GroupCreatorEmail { get; set; } = "";

    public async Task<IActionResult> OnGetAsync(int? userId, int? groupId)
    {
        if (userId == null || groupId == null)
            return NotFound();

        // get user + group data
        UserGroup = await _context.UserGroup
            .Include(ug => ug.User)
            .Include(ug => ug.Group)
                .ThenInclude(g => g.CreatedByUser)
            .FirstOrDefaultAsync(m => m.UserId == userId && m.GroupId == groupId);

        if (UserGroup == null)
            return NotFound();

        // get member count
        GroupMemberCount = await _context.UserGroup.CountAsync(ug => ug.GroupId == groupId);

        // get creator email
        GroupCreatorEmail = UserGroup.Group.CreatedByUser.Email;

        return Page();
    }
}
