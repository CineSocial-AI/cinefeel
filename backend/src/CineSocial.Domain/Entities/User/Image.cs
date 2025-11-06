using CineSocial.Domain.Common;

namespace CineSocial.Domain.Entities.User;

public class Image : BaseEntity
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public long Size { get; set; }
}
