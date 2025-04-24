using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using DM2Projekt.Data;
using DM2Projekt.Models;

namespace DM2Projekt.Pages.Bookings
{
    public class CreateModel : PageModel
    {
        private readonly DM2Projekt.Data.DM2ProjektContext _context;

        public CreateModel(DM2Projekt.Data.DM2ProjektContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            ViewData["GroupId"] = new SelectList(_context.Group, "GroupId", "GroupName");
            ViewData["RoomId"] = new SelectList(_context.Room, "RoomId", "RoomName");
            ViewData["SmartboardId"] = new SelectList(_context.Smartboard, "SmartboardId", "SmartboardId");
            ViewData["CreatedByUserId"] = new SelectList(_context.User, "UserId", "Email");

            return Page();
        }

        [BindProperty]
        public Booking Booking { get; set; } = default!;

        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
           // if (!ModelState.IsValid)
            //{
              //  return Page();
            //}

            // Tjek at varigheden ikke overstiger 2 timer
            TimeSpan bookingLength = Booking.EndTime - Booking.StartTime;
            if (bookingLength.TotalHours > 2)
            {
                ModelState.AddModelError(string.Empty, "En booking må maksimalt vare 2 timer.");
                return Page();
            }

            // Tjek for overlap med eksisterende bookinger
            bool overlaps = _context.Booking.Any(b =>
                b.RoomId == Booking.RoomId &&
                ((Booking.StartTime >= b.StartTime && Booking.StartTime < b.EndTime) ||
                 (Booking.EndTime > b.StartTime && Booking.EndTime <= b.EndTime) ||
                 (Booking.StartTime <= b.StartTime && Booking.EndTime >= b.EndTime)));

            if (overlaps)
            {
                ModelState.AddModelError(string.Empty, "Lokalet er allerede booket i det valgte tidsrum.");
                return Page();
            }

            _context.Booking.Add(Booking);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
