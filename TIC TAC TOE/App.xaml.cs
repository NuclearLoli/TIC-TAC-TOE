using System.Windows;
using Microsoft.EntityFrameworkCore;
using TIC_TAC_TOE.Data;

namespace TIC_TAC_TOE
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            using AppDbContext dbContext = new();
            dbContext.Database.Migrate();
        }
    }
}
