using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Gms.Auth.Api.SignIn;
using Android.Gms.Common.Apis;
using Android.Gms.Auth.Api;
using Android.Support.V4.App;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;
using Android.Gms.Common;

namespace AndroidDemo
{
    [Activity(Label = "AndroidDemo", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        // For sign in to work you must register your app for OAuth 2.0 Client access in the Google Play Developer API
        // Helpful links:
        //  https://console.developers.google.com/apis/credentials
        //  https://developer.xamarin.com/guides/android/deployment,_testing,_and_metrics/MD5_SHA1/
        //  https://components.xamarin.com/gettingstarted/googleplayservices-drive

        GoogleApiClient _googleApiClient;

        int _nextIntentRequestCode;
        ConcurrentDictionary<int, TaskCompletionSource<ActivityResult>> _activityResultTasks = new ConcurrentDictionary<int, TaskCompletionSource<ActivityResult>>();

        class ActivityResult
        {
            public Result ResultCode { get; set; }
            public Intent Data { get; set; }
        }

        TaskCompletionSource<ActivityResult> GetActivityResultTask(out int requestCode)
        {
            var tcs = new TaskCompletionSource<ActivityResult>();
            requestCode = Interlocked.Increment(ref _nextIntentRequestCode);
            if (_activityResultTasks.TryAdd(requestCode, tcs))
            {
                return tcs;
            }
            else
            {
                throw new Exception("Failed to add request result task");
            }
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            var gso = new GoogleSignInOptions.Builder(GoogleSignInOptions.DefaultSignIn)
                .RequestEmail()
                .Build();

            _googleApiClient = new GoogleApiClient.Builder(this)
                .AddConnectionCallbacks(b => {
                    // connected
                    Console.WriteLine("Google API client connected");
                }, result => {
                    // connection suspended
                    Console.WriteLine("Google API client connection suspended " + result);
                })
                .AddOnConnectionFailedListener(ConnectionFailedHandler)
                .AddApi(Auth.GOOGLE_SIGN_IN_API, gso)
                .Build();

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            // Get our button from the layout resource,
            // and attach an event to it
            Button button = FindViewById<Button>(Resource.Id.MyButton);

            button.Click += (s, e) => Test();
        }



        async void Test()
        {

            Intent signInIntent = Auth.GoogleSignInApi.GetSignInIntent(_googleApiClient);

            int signInRequestCode;
            var signInRequestTask = GetActivityResultTask(out signInRequestCode);
            StartActivityForResult(signInIntent, signInRequestCode);

            var signInIntentResult = await signInRequestTask.Task;

            if (signInIntentResult.ResultCode != Result.Ok)
            {
                throw new Exception("Sign in failed " + signInIntentResult.ResultCode);
            }

            var signInResult = Auth.GoogleSignInApi.GetSignInResultFromIntent(signInIntentResult.Data);

            if (!signInResult.IsSuccess)
            {
                throw new Exception("Sign in result was not successful");
            }


        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            TaskCompletionSource<ActivityResult> activityResult;
            if (_activityResultTasks.TryRemove(requestCode, out activityResult))
            {
                activityResult.TrySetResult(new ActivityResult { ResultCode = resultCode, Data = data });
            }
            base.OnActivityResult(requestCode, resultCode, data);
        }

        async void ConnectionFailedHandler(ConnectionResult result)
        {
            if (result.HasResolution)
            {
                Console.WriteLine("Google API client connection failed, attempting resultion");
                int requestCode;
                var tcs = GetActivityResultTask(out requestCode);
                result.StartResolutionForResult(this, requestCode);
                var resolveResult = await tcs.Task;
                if (resolveResult.ResultCode == Result.Ok)
                {
                    Console.WriteLine("Sign in resolution successful");
                    if (!_googleApiClient.IsConnecting && !_googleApiClient.IsConnected)
                    {
                        _googleApiClient.Connect();
                    }
                }
                else
                {
                    Console.WriteLine("Sign in resolution failed " + resolveResult.ResultCode);
                }
            }
            else
            {
                Console.WriteLine($"Google API client  connection failed without resolution, error {result.ErrorCode}: {result.ErrorMessage}");
            }
        }
    }
}

