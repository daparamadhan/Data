using Datas.Forms;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Datas
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static IMongoDatabase Database;

        [STAThread]
        static void Main()
        {
            // Setup MongoDB connection
            string connectionString = "mongodb+srv://<username>:<password>@cluster0.mongodb.net/<database>?retryWrites=true&w=majority";
            var client = new MongoClient(connectionString);
            Database = client.GetDatabase("<database>");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
