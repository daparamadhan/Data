Instructions for this project:

1. Required NuGet packages (install via Package Manager Console or Visual Studio NuGet UI):
   - MongoDB.Driver
   - iTextSharp (version 5.5.13.3 recommended)

   Example Package Manager Console commands:
   PM> Install-Package MongoDB.Driver
   PM> Install-Package iTextSharp -Version 5.5.13.3

2. MongoDB:
   - By default the app uses "mongodb://localhost:27017" and database "datasdb" with collection "sampah".
   - Change connection string in `Form1` constructor if needed.

3. Run the application from Visual Studio 2022.

4. The UI is simple: CRUD grid on left, form inputs and buttons on right, chatbot at bottom.

5. Security: ensure you do not commit credentials into source control.

6. For production or advanced features, consider using async MongoDB driver methods and better error handling.
