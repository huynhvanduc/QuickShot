namespace QuickShot;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        // MainWindow hiện trên taskbar (thu nhỏ sẵn) để người dùng biết app đang chạy,
        // đồng thời vẫn giữ icon + menu ở khay hệ thống.
        Application.Run(new MainWindow());
    }
}
