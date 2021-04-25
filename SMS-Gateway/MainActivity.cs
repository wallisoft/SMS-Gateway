using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using Android.Content.PM;
using Android.Telephony;
using System;
using Android;
using Android.Content;
using Android.Support.V4.Content;
using Android.Support.V4.App;
using Android.Util;
using Android.Support.Design.Widget;
using Android.Views;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit;
using MimeKit;
using Android.Provider;
using System.Threading;
using Android.Icu.Text;
using System.Threading.Tasks;

namespace SMS_Gateway
{
    [Activity(Label = "SMS-Gateway", Theme = "@style/AppTheme", MainLauncher = true)]


    public class MainActivity : AppCompatActivity, ActivityCompat.IOnRequestPermissionsResultCallback 
    {
        static readonly int REQUEST_SENDSMS = 0;
        View layout;
        private SmsManager _smsManager;
        private BroadcastReceiver _smsSentBroadcastReceiver, _smsDeliveredBroadcastReceiver;

        public static Context Instance { get; internal set; }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.layout1);
            layout = FindViewById(Resource.Id.sample_main_layout);

            CheckBox startServiceCheckBox = FindViewById<CheckBox>(Resource.Id.checkBox1);

            startServiceCheckBox.CheckedChange += startServiceCheckBox_CheckedChange;


            Button smsBtn = FindViewById<Button>(Resource.Id.btnSend);
            EditText txtPhoneNum = FindViewById<EditText>(Resource.Id.txtPhoneNumber);
            EditText txtServer = FindViewById<EditText>(Resource.Id.txtEmailServer);
            EditText txtPort = FindViewById<EditText>(Resource.Id.txtServerPort);
            EditText txtUser = FindViewById<EditText>(Resource.Id.txtUsername);
            EditText txtPass = FindViewById<EditText>(Resource.Id.txtPassword);

            txtPhoneNum.Text = GetString("Phone");
            txtServer.Text = GetString("Server");
            txtPort.Text = GetString("Port");
            txtUser.Text = GetString("User");
            txtPass.Text = GetString("Pass");

            _smsManager = SmsManager.Default;




            smsBtn.Click += (s, e) =>
            {
                smsBtn.Text = "Service Started";

                SaveString("Phone", txtPhoneNum.Text);
                SaveString("Server", txtServer.Text);
                SaveString("Port", txtPort.Text);
                SaveString("User", txtUser.Text);
                SaveString("Pass", txtPass.Text);

                MainLoop();

            };

            void startServiceCheckBox_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
            {
                if (e.IsChecked)
                {
                    this.Window.SetFlags(WindowManagerFlags.KeepScreenOn, WindowManagerFlags.AllowLockWhileScreenOn);
                }
                else
                {
                }
            }
        }


        private void MainLoop()
        {
            EditText txtPhoneNum = FindViewById<EditText>(Resource.Id.txtPhoneNumber);
            Alarm alarm = new Alarm();
            RegisterReceiver(alarm, new IntentFilter("com.company.BROADCAST"));
            alarm.SetAlarm(this);
            Console.WriteLine("Alarm made: " + this.GetHashCode());

            SndSms(txtPhoneNum.Text, "The sms-gateway service has started on this device. If your device sleeps messages will only be sent once it wakes.");

        }

        public string GetString(string key)
        {
            var prefs = this.GetSharedPreferences(this.PackageName, FileCreationMode.Private);
            return prefs.GetString(key, string.Empty);
        }

        public void SaveString(string key, string value)
        {
            var prefs = this.GetSharedPreferences(this.PackageName, FileCreationMode.Private);
            var prefEditor = prefs.Edit();
            prefEditor.PutString(key, value);
            prefEditor.Commit();
        }

        public void SndSms(string phone, string message)
        {
            var piSent = PendingIntent.GetBroadcast(this, 0, new Intent("SMS_SENT"), 0);
            var piDelivered = PendingIntent.GetBroadcast(this, 0, new Intent("SMS_DELIVERED"), 0);

            if ((int)Build.VERSION.SdkInt < 23)
            {
                _smsManager.SendTextMessage(phone, null, message, piSent, piDelivered);
                return;
            }
            else
            {
                if (ActivityCompat.CheckSelfPermission(this, Manifest.Permission.SendSms) != (int)Permission.Granted)
                {
                    // Permission is not granted. If necessary display rationale & request.
                    RequestSendSMSPermission();
                }
                else
                {
                    // We have permission, go ahead and send SMS.
                    _smsManager.SendTextMessage(phone, null, message, piSent, piDelivered);
                }
            }
        }

        protected override void OnResume()
        {
            base.OnResume();

            _smsSentBroadcastReceiver = new SMSSentReceiver();
            _smsDeliveredBroadcastReceiver = new SMSDeliveredReceiver();

            RegisterReceiver(_smsSentBroadcastReceiver, new IntentFilter("SMS_SENT"));
            RegisterReceiver(_smsDeliveredBroadcastReceiver, new IntentFilter("SMS_DELIVERED"));
        }

        protected override void OnPause()
        {
            base.OnPause();

            UnregisterReceiver(_smsSentBroadcastReceiver);
            UnregisterReceiver(_smsDeliveredBroadcastReceiver);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        void RequestSendSMSPermission()
        {
            Log.Info("MainActivity", "Message permission has NOT been granted. Requesting permission.");

            if (ActivityCompat.ShouldShowRequestPermissionRationale(this, Manifest.Permission.SendSms))
            {
                // Provide an additional rationale to the user if the permission was not granted
                // and the user would benefit from additional context for the use of the permission.
                // For example if the user has previously denied the permission.
                Log.Info("MainActivity", "Displaying message permission rationale to provide additional context.");
                //Activity activity = CrossCurrentActivity.Current.Activity;
                //Android.Views.View activityRootView = activity.FindViewById(Android.Resource.Id.Content);
                Snackbar.Make(layout, "Message permission is needed to send SMS.",
                    Snackbar.LengthIndefinite).SetAction("OK", new Action<View>(delegate (View obj) {
                        ActivityCompat.RequestPermissions(this, new System.String[] { Manifest.Permission.SendSms }, REQUEST_SENDSMS);
                    })).Show();
            }
            else
            {
                // Message permission has not been granted yet. Request it directly.
                ActivityCompat.RequestPermissions(this, new System.String[] { Manifest.Permission.SendSms }, REQUEST_SENDSMS);
            }
        }
    }

    [BroadcastReceiver]
    public class SMSSentReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            switch ((int)ResultCode)
            {
                case (int)Result.Ok:
                    Toast.MakeText(Application.Context, "SMS has been sent", ToastLength.Short).Show();
                    break;
                case (int)SmsResultError.GenericFailure:
                    Toast.MakeText(Application.Context, "Generic Failure", ToastLength.Short).Show();
                    break;
                case (int)SmsResultError.NoService:
                    Toast.MakeText(Application.Context, "No Service", ToastLength.Short).Show();
                    break;
                case (int)SmsResultError.NullPdu:
                    Toast.MakeText(Application.Context, "Null PDU", ToastLength.Short).Show();
                    break;
                case (int)SmsResultError.RadioOff:
                    Toast.MakeText(Application.Context, "Radio Off", ToastLength.Short).Show();
                    break;
            }
        }
    }

    [BroadcastReceiver]
    public class SMSDeliveredReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            switch ((int)ResultCode)
            {
                case (int)Result.Ok:
                    Toast.MakeText(Application.Context, "SMS Delivered", ToastLength.Short).Show();
                    break;
                case (int)Result.Canceled:
                    Toast.MakeText(Application.Context, "SMS not delivered", ToastLength.Short).Show();
                    break;
            }
        }


    }


    [BroadcastReceiver]

    [IntentFilter(new string[] { "com.company.BROADCAST" })]
    public class Alarm : BroadcastReceiver
    {
        private SmsManager _smsManager1;

        public override void OnReceive(Context c, Intent i)
        {

            Console.WriteLine("Alarm made: " + this.GetHashCode());
            CheckMail(c);
        }

        public string GetString(Context c, string key)
        {
            var prefs = c.GetSharedPreferences(c.PackageName, FileCreationMode.Private);
            return prefs.GetString(key, string.Empty);
        }

        public void CheckMail(Context c)
        {
            _smsManager1 = SmsManager.Default;
            var client = new ImapClient();
            var piSent = PendingIntent.GetBroadcast(c, 0, new Intent("SMS_SENT"), 0);
            var piDelivered = PendingIntent.GetBroadcast(c, 0, new Intent("SMS_DELIVERED"), 0);

            var strServer = GetString(c, "Server");
            var strPort = GetString(c, "Port");
            var strUser = GetString(c, "User");
            var strPass = GetString(c, "Pass");

            client.Connect(strServer, Convert.ToInt32(strPort), true);
            client.Authenticate(strUser, strPass);

            // The Inbox folder is always available on all IMAP servers...

            var inbox = client.Inbox;
            inbox.Open(FolderAccess.ReadWrite);

            var uids = inbox.Search(SearchQuery.NotSeen);

            foreach (var uid in uids)
            {
                var message = inbox.GetMessage(uid);

                var num = message.Subject;
                var msg = message.TextBody;

                num = num.Replace(" ", "");

                string[] lines = num.Split(':');

                if (lines[0] == "SMS")
                {
                    num = lines[1];
                    inbox.AddFlags(uid, MessageFlags.Seen, true);
                    _smsManager1.SendTextMessage(num, null, msg, piSent, piDelivered);

                }
            }

            client.Disconnect(true);
        }

        public void SetAlarm(Context context)
        {
            AlarmManager am = (AlarmManager)context.GetSystemService(Context.AlarmService);
            Intent i = new Intent("com.company.BROADCAST");
            PendingIntent pi = PendingIntent.GetBroadcast(context, 0, i, 0);
            am.SetRepeating(AlarmType.ElapsedRealtimeWakeup, SystemClock.ElapsedRealtime() + 1000, 1000 * 60, pi);
        }
    }
    
}