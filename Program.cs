using System;
using System.Threading.Tasks;
using System.Windows;

namespace TextLabClient
{
    public class Program
    {
        [STAThread]
        public static async Task Main(string[] args)
        {
            // Si argument "test" est passé, lancer le diagnostic
            if (args.Length > 0 && args[0].ToLower() == "test")
            {
                Console.WriteLine("Mode diagnostic activé");
                await TestDocumentCreation.RunTest();
                return;
            }
            
            // Sinon, lancer l'interface graphique normale
            var app = new App();
            app.InitializeComponent();
            app.Run();
        }
    }
} 