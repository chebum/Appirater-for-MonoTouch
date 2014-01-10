using System;
using System.Diagnostics;
using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;
using System.Threading.Tasks;


public class AppiraterSettings
{
	/*
 		 Place your Apple generated software id here.
 		 */
	public int AppId;

	/*
 		 Your app's name.
 		 */
	public string AppName;

	/*
 		 This is the message your users will see once they've passed the day+launches
 		 threshold.
 		 */
	public string Message;

	/*
 		 This is the title of the message alert that users will see.
 		 */
	public string MessageTitle;

	/*
 		 The text of the button that rejects reviewing the app.
 		 */
	public string CancelButton;

	/*
		 Text of button that will send user to app review page.
		 */
	public string RateButton;

	/*
		 Text for button to remind the user to review later.
		 */
	public string RateLaterButton;

	/*
		 Users will need to have the same version of your app installed for this many
		 days before they will be prompted to rate it.
		 */
	public int DaysUntilPrompt;

	/*
		 An example of a 'use' would be if the user launched the app. Bringing the app
		 into the foreground (on devices that support it) would also be considered
		 a 'use'. You tell Appirater about these events using the two methods:
		 [Appirater appLaunched:]
		 [Appirater appEnteredForeground:]

		 Users need to 'use' the same version of the app this many times before
		 before they will be prompted to rate it.
		 */
	public int UsesUntiPrompt;

	/*
		 A significant event can be anything you want to be in your app. In a
		 telephone app, a significant event might be placing or receiving a call.
		 In a game, it might be beating a level or a boss. This is just another
		 layer of filtering that can be used to make sure that only the most
		 loyal of your users are being prompted to rate you on the app store.
		 If you leave this at a value of -1, then this won't be a criteria
		 used for rating. To tell Appirater that the user has performed
		 a significant event, call the method:
		 [Appirater userDidSignificantEvent:];
		 */
	public int SigEventsUntilPrompt;

	/*
		 Once the rating alert is presented to the user, they might select
		 'Remind me later'. This value specifies how long (in days) Appirater
		 will wait before reminding them.
		 */
	public int TimeBeforeReminding;

	/*
		 'YES' will show the Appirater alert everytime. Useful for testing how your message
		 looks and making sure the link to your app's review page works.
		 */
	public bool Debug;

	public AppiraterSettings (int appId)
			: this (appId, (NSString) NSBundle.MainBundle.InfoDictionary.ObjectForKey (new NSString ("CFBundleName")), false)
	{
	}

	public AppiraterSettings (int appId, bool debug)
			: this (appId, (NSString) NSBundle.MainBundle.InfoDictionary.ObjectForKey (new NSString ("CFBundleName")), debug)
	{
	}

	public AppiraterSettings (int appId, string appName, bool debug)
	{
		AppId = appId;
		AppName = appName;
		Message = string.Format ("If you enjoy using {0}, would you mind taking a moment to rate it? It won't take more than a minute. Thanks for your support!", AppName);
		MessageTitle = string.Format ("Rate {0}", AppName);
		CancelButton = "No, Thanks";
		RateButton = string.Format ("Rate {0}", AppName);
		RateLaterButton = "Remind me later";
		DaysUntilPrompt = 30;
		UsesUntiPrompt = 20;
		SigEventsUntilPrompt = -1;
		TimeBeforeReminding = 1;
		Debug = debug;
	}
}

public class Appirater : NSObject
{
	const string SELECTOR_INCREMENT_AND_RATE = "incrementAndRate";
	const string SELECTOR_INCREMENT_EVENT_AND_RATE = "incrementSignificantEventAndRate";
	const string FIRST_USE_DATE = "kAppiraterFirstUseDate";
	const string USE_COUNT = "kAppiraterUseCount";
	const string SIGNIFICANT_EVENT_COUNT = "kAppiraterSignificantEventCount";
	const string CURRENT_VERSION = "kAppiraterCurrentVersion";
	const string RATED_CURRENT_VERSION = "kAppiraterRatedCurrentVersion";
	const string DECLINED_TO_RATE = "kAppiraterDeclinedToRate";
	const string REMINDER_REQUEST_DATE = "kAppiraterReminderRequestDate";
	const string TEMPLATE_REVIEW_URL = "macappstore://itunes.apple.com/app/id{0}?mt=12";
	                                   
	readonly AppiraterSettings settings;
	NSAlert ratingAlert;

	public Appirater (int appId)
			: this (new AppiraterSettings (appId))
	{
	}

	public Appirater (int appId, bool debug)
			: this (new AppiraterSettings (appId, debug))
	{
	}

	public Appirater (AppiraterSettings settings)
	{
		this.settings = settings;

	}

	public NSAlert RatingAlert { get { return ratingAlert; } }

	/*
		 * Returns current app version
		 */
	public string CurrentVersion {
		get {
			return (NSString) NSBundle.MainBundle.InfoDictionary.ObjectForKey (new NSString ("CFBundleVersion"));
		}
	}

	/*
		 DEPRECATED: While still functional, it's better to use
		 appLaunched:(BOOL)canPromptForRating instead.

		 Calls [Appirater appLaunched:YES]. See appLaunched: for details of functionality.
		 */
	public void AppLaunched ()
	{
		AppLaunched (true);
	}
		
	/*
		 Tells Appirater that the app has launched, and on devices that do NOT
		 support multitasking, the 'uses' count will be incremented. You should
		 call this method at the end of your application delegate's
		 application:didFinishLaunchingWithOptions: method.
		 
		 If the app has been used enough to be rated (and enough significant events),
		 you can suppress the rating alert
		 by passing NO for canPromptForRating. The rating alert will simply be postponed
		 until it is called again with YES for canPromptForRating. The rating alert
		 can also be triggered by appEnteredForeground: and userDidSignificantEvent:
		 (as long as you pass YES for canPromptForRating in those methods).
		 */
	public void AppLaunched (bool canPromptForRating)
	{
		Task.Factory.StartNew( () => IncrementAndRate (NSNumber.FromBoolean (canPromptForRating)));

	}

	/*
		 Tells Appirater that the user performed a significant event. A significant
		 event is whatever you want it to be. If you're app is used to make VoIP
		 calls, then you might want to call this method whenever the user places
		 a call. If it's a game, you might want to call this whenever the user
		 beats a level boss.
		 
		 If the user has performed enough significant events and used the app enough,
		 you can suppress the rating alert by passing NO for canPromptForRating. The
		 rating alert will simply be postponed until it is called again with YES for
		 canPromptForRating. The rating alert can also be triggered by appLaunched:
		 and appEnteredForeground: (as long as you pass YES for canPromptForRating
		 in those methods).
		 */
	public void UserDidSignificantEvent (bool canPromptForRating)
	{
		Task.Factory.StartNew( () => IncrementSignificantEventAndRate (NSNumber.FromBoolean (canPromptForRating)));

	}

	/*
		 Tells Appirater to open the App Store page where the user can specify a
		 rating for the app. Also records the fact that this has happened, so the
		 user won't be prompted again to rate the app.

		 The only case where you should call this directly is if your app has an
		 explicit "Rate this app" command somewhere.  In all other cases, don't worry
		 about calling this -- instead, just call the other functions listed above,
		 and let Appirater handle the bookkeeping of deciding when to ask the user
		 whether to rate the app.
		 */
	public void RateApp ()
	{

			NSUserDefaults userDefaults = NSUserDefaults.StandardUserDefaults;
			string reviewURL = string.Format (TEMPLATE_REVIEW_URL, settings.AppId);
			userDefaults.SetBool (true, RATED_CURRENT_VERSION);
			userDefaults.Synchronize ();

		NSWorkspace.SharedWorkspace.OpenUrl(NSUrl.FromString (reviewURL));

	}

	/*
		 * Restarts tracking
		 */
	public void Restart ()
	{
		string version = (NSString) NSBundle.MainBundle.InfoDictionary.ObjectForKey (new NSString ("CFBundleVersion"));

		NSUserDefaults userDefaults = NSUserDefaults.StandardUserDefaults;
		userDefaults.SetString (version, CURRENT_VERSION);
		userDefaults.SetDouble (DateTime.Now.ToOADate (), FIRST_USE_DATE);
		userDefaults.SetInt (1, USE_COUNT);
		userDefaults.SetInt (0, SIGNIFICANT_EVENT_COUNT);
		userDefaults.SetBool (false, RATED_CURRENT_VERSION);
		userDefaults.SetBool (false, DECLINED_TO_RATE);
		userDefaults.SetDouble (0, REMINDER_REQUEST_DATE);
	}


	bool ConnectedToNetwork ()
	{
		return Reachability.InternetConnectionStatus () != NetworkStatus.NotReachable;
	}

	void ShowRatingAlert ()
	{
		NSAlert alertView =NSAlert.WithMessage(settings.MessageTitle, settings.CancelButton, settings.RateButton, settings.RateLaterButton, settings.Message);


	
		ratingAlert = alertView;
		var buttonId = alertView.RunModal ();

		HandleClick (buttonId);

	}

	public  void HandleClick (int  buttonIndex)
			{

				NSUserDefaults userDefaults = NSUserDefaults.StandardUserDefaults;
				switch (buttonIndex) {
					case -1:
						// remind them later
						userDefaults.SetDouble (DateTime.Now.ToOADate (), REMINDER_REQUEST_DATE);
						userDefaults.Synchronize ();
						break;

					case 0:
						// they want to rate it
						RateApp ();
						break;

					case 1:
						// they don't want to rate it
						userDefaults.SetBool (true, DECLINED_TO_RATE);
						userDefaults.Synchronize ();
						break;
				}
			}

	bool RatingConditionsHaveBeenMet ()
	{
		if (settings.Debug)
			return true;

		NSUserDefaults userDefaults = NSUserDefaults.StandardUserDefaults;
		DateTime dateOfFirstLaunch = DateTime.FromOADate (userDefaults.DoubleForKey (FIRST_USE_DATE));
		TimeSpan timeSinceFirstLaunch = DateTime.Now.Subtract (dateOfFirstLaunch);
		TimeSpan timeUntilRate = new TimeSpan (settings.DaysUntilPrompt, 0, 0, 0);
		if (timeSinceFirstLaunch < timeUntilRate)
			return false;

		// check if the app has been used enough
		int useCount = userDefaults.IntForKey (USE_COUNT);
		if (useCount < settings.UsesUntiPrompt)
			return false;

		// check if the user has done enough significant events
		int sigEventCount = userDefaults.IntForKey (SIGNIFICANT_EVENT_COUNT);
		if (sigEventCount < settings.SigEventsUntilPrompt)
			return false;

		// has the user previously declined to rate this version of the app?
		if (userDefaults.BoolForKey (DECLINED_TO_RATE))
			return false;

		// has the user already rated the app?
		if (userDefaults.BoolForKey (RATED_CURRENT_VERSION))
			return false;

		// if the user wanted to be reminded later, has enough time passed?
		DateTime reminderRequestDate = DateTime.FromOADate (userDefaults.DoubleForKey (REMINDER_REQUEST_DATE));
		TimeSpan timeSinceReminderRequest = DateTime.Now.Subtract (reminderRequestDate);
		TimeSpan timeUntilReminder = new TimeSpan (settings.TimeBeforeReminding, 0, 0, 0);
		if (timeSinceReminderRequest < timeUntilReminder)
			return false;

		return true;
	}

	void IncrementUseCount ()
	{
		// get the app's version
		string version = CurrentVersion;

		// get the version number that we've been tracking
		NSUserDefaults userDefaults = NSUserDefaults.StandardUserDefaults;
		string trackingVersion = userDefaults.StringForKey (CURRENT_VERSION);
		if (string.IsNullOrEmpty (trackingVersion)) {
			trackingVersion = version;
			userDefaults.SetString (version, CURRENT_VERSION);
		}

		if (settings.Debug)
			Debug.WriteLine ("APPIRATER Tracking version: {0}", trackingVersion);

		if (trackingVersion == version) {
			// check if the first use date has been set. if not, set it.
			double timeInterval = userDefaults.DoubleForKey (FIRST_USE_DATE);
			if (timeInterval == 0) {
				timeInterval = DateTime.Now.ToOADate ();
				userDefaults.SetDouble (timeInterval, FIRST_USE_DATE);
			}

			// increment the use count
			int useCount = userDefaults.IntForKey (USE_COUNT);
			useCount ++;
			userDefaults.SetInt (useCount, USE_COUNT);
			if (settings.Debug)
				Debug.WriteLine ("APPIRATER Use count: {0}", useCount);
		} else
			Restart ();

		userDefaults.Synchronize ();
	}

	void IncrementSignificantEventCount ()
	{
		// get the app's version
		string version = CurrentVersion;

		// get the version number that we've been tracking
		NSUserDefaults userDefaults = NSUserDefaults.StandardUserDefaults;
		string trackingVersion = userDefaults.StringForKey (CURRENT_VERSION);
		if (string.IsNullOrEmpty (trackingVersion)) {
			trackingVersion = version;
			userDefaults.SetString (version, CURRENT_VERSION);
		}

		if (settings.Debug)
			Debug.WriteLine ("APPIRATER Tracking version: {0}", trackingVersion);

		if (trackingVersion == version) {
			// check if the first use date has been set. if not, set it.
			double timeInterval = userDefaults.DoubleForKey (FIRST_USE_DATE);
			if (timeInterval == 0) {
				timeInterval = DateTime.Now.ToOADate ();
				userDefaults.SetDouble (timeInterval, FIRST_USE_DATE);
			}

			// increment the significant event count
			int sigEventCount = userDefaults.IntForKey (SIGNIFICANT_EVENT_COUNT);
			sigEventCount ++;
			userDefaults.SetInt (sigEventCount, SIGNIFICANT_EVENT_COUNT);
			if (settings.Debug)
				Debug.WriteLine ("APPIRATER Significant event count: {0}", sigEventCount);
		} else
			Restart ();

		userDefaults.Synchronize ();
	}

	[Export(SELECTOR_INCREMENT_AND_RATE)]
	void IncrementAndRate (NSNumber _canPromptForRating)
	{
		using (new NSAutoreleasePool ()) {
			IncrementUseCount ();
			if (_canPromptForRating.BoolValue && RatingConditionsHaveBeenMet () && ConnectedToNetwork ())
				InvokeOnMainThread (ShowRatingAlert);
		}
	}

	[Export(SELECTOR_INCREMENT_EVENT_AND_RATE)]
	void IncrementSignificantEventAndRate (NSNumber _canPromptForRating)
	{
		using (new NSAutoreleasePool ()) {
			IncrementSignificantEventCount ();
			if (_canPromptForRating.BoolValue && RatingConditionsHaveBeenMet () && ConnectedToNetwork ())
				InvokeOnMainThread (ShowRatingAlert);
		}
	}


}