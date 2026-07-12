namespace HappyWorld.HappyPlace.Web.Models.ChatMedia;

public record ChatMediaUploadModel(string AuthToken, Guid ChatGroupId, byte Kind, int DurationSeconds, IFormFile Media) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public ChatMediaUploadResult Upload() {
        return ChatMediaManager.Upload(this.AuthToken, this.ChatGroupId, this.Kind, this.DurationSeconds, ReadMediaBytes());
    }

    // Helpers

    private byte[] ReadMediaBytes() {
        if (this.Media == null || this.Media.Length == 0)
            return null;
        using var memoryStream = new MemoryStream();
        this.Media.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }
}
