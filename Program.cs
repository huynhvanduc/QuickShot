namespace QuickShot;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        // Chạy bằng ApplicationContext (không có main form) => app ẩn hoàn toàn,
        // chỉ hiện icon dưới khay hệ thống.
        Application.Run(new TrayContext());
    }
}
