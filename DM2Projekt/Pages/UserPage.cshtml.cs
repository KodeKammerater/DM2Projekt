using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DM2Projekt.Models;
using DM2Projekt.Data;
using DM2Projekt.Pages.Bookings;
using Microsoft.AspNetCore.Mvc;

namespace DM2Projekt.Pages
{
    public class UserPageModel : PageModel
    {
        private readonly DM2ProjektContext _context;

        public UserPageModel(DM2ProjektContext context)
        {
            _context = context;
        }

        public User User { get; set; } = default!;
        public List<Booking> Bookings { get; set; } = new();
        public List<Group> Groups { get; set; } = new();
        public Booking? SelectedBooking { get; set; }

        public async Task OnGetAsync(int? bookingId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                RedirectToPage("/Login");
                return;
            }

            // Fetch user details
            User = await _context.User
                .Include(u => u.UserGroups)
                .ThenInclude(ug => ug.Group)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (User == null)
            {
                RedirectToPage("/Login");
                return;
            }

            // Get the bookings for the user
            Bookings = await _context.Booking
                .Include(b => b.Room)
                .Include(b => b.Group)
                .Where(b => b.CreatedByUserId == userId)
                .ToListAsync();

            // Get the groups the user is part of
            Groups = User.UserGroups.Select(ug => ug.Group).ToList();

            if (bookingId.HasValue)
            {
                SelectedBooking = await _context.Booking
                    .Include(b => b.Room)
                    .Include(b => b.Group)
                    .Include(b => b.CreatedByUser)
                    .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.CreatedByUserId == userId);
            }


        }

        public async Task<IActionResult> OnPostDeleteAsync(int bookingId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToPage("/Login");

            var booking = await _context.Booking
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.CreatedByUserId == userId);
            if (booking != null)
            {
                _context.Booking.Remove(booking);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }
    }
}
