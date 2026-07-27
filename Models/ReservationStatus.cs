using Microsoft.AspNetCore.Identity;

namespace LibraryManagement.Models
{
    public enum ReservationStatus
    {
        Pending,
        Approved,
        Rejected,
        Cancelled
    }
}