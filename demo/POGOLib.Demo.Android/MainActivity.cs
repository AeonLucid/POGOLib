using System;
using Android.App;
using Android.OS;
using Android.Widget;
using Google.Protobuf;
using Newtonsoft.Json;
using POGOProtos.Networking.Requests;
using POGOProtos.Networking.Requests.Messages;
using POGOProtos.Networking.Responses;

namespace POGOLib.Demo.Android
{
    [Activity(Label = "POGOLib Android Demo", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        private TextView _outputText;
        private Button _loginButton;
        private EditText _username;
        private EditText _password;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            SetContentView(Resource.Layout.Main);

            _outputText = FindViewById<TextView>(Resource.Id.TextViewOutput);
            _username = FindViewById<EditText>(Resource.Id.EditTextUsername);
            _password = FindViewById<EditText>(Resource.Id.EditTextPassword);
            _loginButton = FindViewById<Button>(Resource.Id.ButtonLogin);
            _loginButton.Click += LoginButtonOnClick;
        }

        private async void LoginButtonOnClick(object sender, EventArgs eventArgs)
        {
            var username = _username.Text;
            var password = _password.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                Toast.MakeText(Application, "Fill in an username and password.", ToastLength.Short).Show();
                return;
            }

            try
            {
                var session = await Login.GetSession(new PtcLoginProvider(username, password), 51.507351, -0.127758);

                await session.StartupAsync();

                var fortDetailsBytes = await session.RpcClient.SendRemoteProcedureCallAsync(new Request
                {
                    RequestType = RequestType.FortDetails,
                    RequestMessage = new FortDetailsMessage
                    {
                        FortId = "e4a5b5a63cf34100bd620c598597f21c.12",
                        Latitude = 51.507335,
                        Longitude = -0.127689
                    }.ToByteString()
                });
                var fortDetailsResponse = FortDetailsResponse.Parser.ParseFrom(fortDetailsBytes);

                _outputText.SetText(JsonConvert.SerializeObject(fortDetailsResponse, Formatting.Indented), TextView.BufferType.Normal);

                session.Shutdown();
            }
            catch (Exception e)
            {
                _outputText.SetText(e.Message, TextView.BufferType.Normal);
            }
        }
    }
}

