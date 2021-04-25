# SMS-Gateway

Xamarin C# SMS Gateway using MailKit. Enter Imap email server credentials and the app will periodically check for new mail and forward the email body as a SMS if the email subject is 'sms: 01323 435644' for example.

An initial message is sent to the device to establish the Application SMS permissions and subsequent messages are processed with a timer. This works when the app is in the background but stops working when the device sleeps. To ensure emails are processed promptly the app can request that the screen is kept alive.

The app works fine on Android. Untested on IOS - let me know if you try it.

TODO
Improve UI.
Add error checking.
Notify user if imap connection fails.
Troubleshoot keep awake functionality.




