namespace RideMatching.Domain.Entities;

public class Rider
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;
    public double Rating { get; private set; } = 5.0;
    public int TotalRides { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Rider() { }

    public Rider(string name, string email, string phoneNumber)
    {
        Id = Guid.NewGuid();
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Email = email ?? throw new ArgumentNullException(nameof(email));
        PhoneNumber = phoneNumber ?? throw new ArgumentNullException(nameof(phoneNumber));
        Rating = 5.0;
        TotalRides = 0;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateProfile(string name, string phoneNumber)
    {
        Name = name;
        PhoneNumber = phoneNumber;
    }

    public void RecordCompletedRide(double riderRating)
    {
        TotalRides++;
        Rating = Math.Round(((Rating * (TotalRides - 1)) + riderRating) / TotalRides, 2);
    }
}
