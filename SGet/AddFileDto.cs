
namespace SGet
{
    public class AddFileDto
    {
        public string Url { get; set; }
        public string SaveFolder { get; set; }
        public string FileName { get; set; }
        public bool ServerLogin { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool StartImmediately { get; set; }
        public bool ShowDialog { get; set; }
        public bool OpenFileOnCompletion { get; set; }
        public AddFileDto()
        {

        }
        public AddFileDto(string Url, string SaveFolder, string FileName,
         bool ServerLogin, string UserName, string Password, bool StartImmediately, bool openFileOnCompletion)
        {
            this.Url = Url;
            this.SaveFolder = SaveFolder;
            this.FileName = FileName;
            this.ServerLogin = ServerLogin;
            this.UserName = UserName;
            this.Password = Password;
            this.StartImmediately = StartImmediately;
            OpenFileOnCompletion = openFileOnCompletion;
        }
    }
}
