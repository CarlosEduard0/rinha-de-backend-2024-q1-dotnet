namespace RinhaBackend;

public static class DateTimeExtensions
{
    public static double ToUnixTimeMicroseconds(this DateTime dateTime) => 
        (dateTime - DateTime.UnixEpoch).TotalMicroseconds;
}
