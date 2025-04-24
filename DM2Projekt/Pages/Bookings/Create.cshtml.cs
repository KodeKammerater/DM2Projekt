using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using DM2Projekt.Data;
using DM2Projekt.Models;
using DM2Projekt.Models.Enums;

namespace DM2Projekt.Pages.Bookings
{
    public class CreateModel : PageModel
    {
        private readonly DM2ProjektContext _context;

        public CreateModel(DM2ProjektContext context)
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

        [BindProperty]
        public string SelectedTimeSlot { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync()
        {
            if (!DateTime.TryParse(SelectedTimeSlot, out var startTime))
            {
                ModelState.AddModelError("SelectedTimeSlot", "Invalid time slot selected.");
                return Page();
            }

            Booking.StartTime = startTime;
            Booking.EndTime = startTime.AddHours(2);

            // Prevent user from booking too many rooms (even in other groups)
            bool userHasAnotherBookingAtSameTime = _context.Booking
                .Any(b =>
                    b.CreatedByUserId == Booking.CreatedByUserId &&
                    b.StartTime < Booking.EndTime &&
                    b.EndTime > Booking.StartTime);

            if (userHasAnotherBookingAtSameTime)
            {
                ModelState.AddModelError(string.Empty, "This user already has a booking at the same time.");
                return Page();
            }

            // Reapply dropdowns to prevent null ViewData on form redisplay
            ViewData["GroupId"] = new SelectList(_context.Group, "GroupId", "GroupName");
            ViewData["RoomId"] = new SelectList(_context.Room, "RoomId", "RoomName");
            ViewData["SmartboardId"] = new SelectList(_context.Smartboard, "SmartboardId", "SmartboardId");
            ViewData["CreatedByUserId"] = new SelectList(_context.User, "UserId", "Email");

            // Extra safety check
            TimeSpan bookingLength = Booking.EndTime - Booking.StartTime;
            if (bookingLength.TotalHours > 2)
            {
                ModelState.AddModelError(string.Empty, "En booking må maksimalt vare 2 timer.");
                return Page();
            }

            // Prevent group from booking multiple rooms at the same time
            bool groupHasOverlappingBooking = _context.Booking
                .Any(b =>
                    b.GroupId == Booking.GroupId &&
                    b.StartTime < Booking.EndTime &&
                    b.EndTime > Booking.StartTime);

            if (groupHasOverlappingBooking)
            {
                ModelState.AddModelError(string.Empty, "This group already has a booking at this time.");
                return Page();
            }

            _context.Booking.Add(Booking);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

        public JsonResult OnGetSmartboardsByRoom(int roomId)
        {
            var smartboards = _context.Smartboard
                .Where(sb => sb.RoomId == roomId && sb.IsAvailable)
                .Select(sb => new
                {
                    sb.SmartboardId,
                    Display = $"Smartboard {sb.SmartboardId}"
                })
                .ToList();

            return new JsonResult(smartboards);
        }

        public JsonResult OnGetAvailableTimeSlots(int roomId, DateTime date)
        {
            var room = _context.Room.FirstOrDefault(r => r.RoomId == roomId);
            if (room == null)
                return new JsonResult(new { error = "Room not found" });

            var slots = GetFixedTimeSlots(date);

            var bookings = _context.Booking
                .Where(b => b.RoomId == roomId &&
                            b.StartTime.Date == date.Date)
                .ToList();

            var availableSlots = new List<object>();

            foreach (var slot in slots)
            {
                var bookingsInSlot = bookings.Count(b =>
                    b.StartTime == slot.start && b.EndTime == slot.end);

                bool isAvailable = room.RoomType switch
                {
                    RoomType.Classroom => bookingsInSlot < 2,
                    RoomType.MeetingRoom => bookingsInSlot < 1,
                    _ => false
                };

                if (isAvailable)
                {
                    availableSlots.Add(new
                    {
                        start = slot.start.ToString("HH:mm"),
                        end = slot.end.ToString("HH:mm"),
                        value = slot.start.ToString("o")
                    });
                }
            }

            return new JsonResult(availableSlots);
        }

        private static List<(DateTime start, DateTime end)> GetFixedTimeSlots(DateTime day)
        {
            var date = day.Date;
            return new List<(DateTime, DateTime)>
            {
                (date.AddHours(8), date.AddHours(10)),
                (date.AddHours(10), date.AddHours(12)),
                (date.AddHours(12), date.AddHours(14)),
                (date.AddHours(14), date.AddHours(16))
            };
        }
    }
}
