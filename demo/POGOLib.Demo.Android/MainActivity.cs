using System;
using Android.App;
using Android.OS;
using Android.Widget;

namespace POGOLib.Demo.Android
{
    [Activity(Label = "POGOLib Android Demo", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        private Button _loginButton;
        private EditText _username;
        private EditText _password;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            
            SetContentView(Resource.Layout.Main);

            _username = FindViewById<EditText>(Resource.Id.EditTextUsername);
            _password = FindViewById<EditText>(Resource.Id.EditTextPassword);
            _loginButton = FindViewById<Button>(Resource.Id.ButtonLogin);
            _loginButton.Click += LoginButtonOnClick;
        }

        private void LoginButtonOnClick(object sender, EventArgs eventArgs)
        {
            var username = _username.Text;
            var password = _password.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                Toast.MakeText(Application, "Fill in an username and password.", ToastLength.Short).Show();
                return;
            }

            // TODO: POGOLib  Login.GetSession(loginProvider, 51.507351, -0.127758)

            Console.WriteLine("Does this work..?");
        }
    }
}

