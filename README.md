exad is an **EX**am **AD**ministration

It is built for the [HTBLA Leonding](https://www.htl-leonding.at), but should also work with other schools. It scrapes exams from [WebUntis](https://webuntis.com), parses their information and can store them in a `.csv` file (which can be imported to [Google Calendar](https://calendar.google.com)) or directly push them to [Google Calendar](https://calendar.google.com) (for that, [Google API](https://developers.google.com/calendar/api/guides/overview) and [Google OAuth 2.0](https://developers.google.com/identity/protocols/oauth2) must be set up).

# Requirements

[.NET 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) or higher

# Setup

1. Download the [source code](https://github.com/antoniosubasic/exad/archive/refs/heads/main.zip)
1. Unzip the folder
1. Run `dotnet run -- config` to generate the config file located in `~/.exad/config.json`
1. In the `config.json` configure the following:
    - `calendar-id` (optional): The ID of the Google Calendar to push the exams to (I recommend [creating a new calendar](https://calendar.google.com/calendar/u/0/r/settings/createcalendar), so the program doesn't have to mess with your personal calendar)
    - `schoolurl`: The URL which WebUnits uses for the school (to get it, go to your school's WebUntis page and copy the domain, e.g.: `mese.webuntis.com`)
    - `schoolname`: The name which WebUnits uses for the school (to get it, go to your school's WebUntis page and copy the name from the top-left corner, e.g.: `HTBLA Linz-Leonding`)
    - `username`: The username of the WebUntis account
    - `password`: The password of the WebUntis account
1. (optional) Setup the Google API:
    - Go to the [Google API Console](https://console.developers.google.com/apis/dashboard) and create a new project - top left: *Select a project* > *New project*. Then, name your project and click *create*
    - Now, go to the [Library](https://console.developers.google.com/apis/library) tab in the side bar and search for "Google Calendar". Click on the *Google Calendar API* card and click *Enable*
    - After that, click on *Credentials* tab under the "Google Calendar API" and then *Configure Consent Screen*
    - Select *External* as the user type. Then fill out the required fields (should only be "App Information" and "Developer Contact Information"). As the app name, you can choose whatever you want and as the email you can choose your normal email (we won't be publishing this project anyway so the contact information isn't important). After that, click *Save and continue*
    - Under Scopes, select *Add or remove scopes* and type filter for "auth/calendar.events" and select the scopes *auth/calendar.events* and *auth/calendar.events.owned* that appear. Then, click *Update* and *Save and continue*
    - Under "Test users", add yourself as the test user (the email you enter will later be used to log into the consent screen), then click *Save and continue*
    - At the end you can review your information and then click *Back to dashboard*
    - Lastly, on go to the [Credentials](https://console.developers.google.com/apis/credentials) tab in the side bar and click *Create credentials*. Select *OAuth client ID*. Select *Desktop app* as the application type, give it a name and click *Create*. A new window will open, showing you the client ID and client secret. Download the JSON file and save at this path in your home directory `~/.exad/client_secrets.json`
1. (optional) Setup a subjects-translator: In the `Classes.cs` there is a method called `TranslateSubject()` which translates the raw subject names from Webuntis any subject names you want. You can change the method to your liking. If you don't want to translate any subjects, just leave the method as it is.

# Usage

Run `dotnet run -- --help` to see the help page

1. `dotnet run -- save -o <output-file>.csv` to save the exams to a `.csv` file (default for output-file is `events.csv`)
1. `dotnet run -- push` to push the exams to the Google Calendar
