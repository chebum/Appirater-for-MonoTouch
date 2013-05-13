Introduction
------------
Appirater is a class that you can drop into any MonoTouch app that will help remind your users
to review your app on the App Store. This project is a port of Obj-C Appirater class by [Arash Payan] [homepage] 

The code is released under the MIT/X11, so feel free to modify and share your changes with the world.

This project uses Reachability class from [Xamarin samples][xamarin]

Getting Started
---------------
1. Add both .cs files into your project.
2. Create a new instance of Appirater: `appirater = new Appirater (<your Apple provided software id>);` at the end of your app delegate's ` DidFinishLaunching ` method.
3. Call `appirater.AppLaunched (true)` at the next line.
4. Call `appirater.AppEnteredForeground (true)` in your app delegate's `WillEnterForeground` method.
5. (OPTIONAL) Call `appirater.UserDidSignificantEvent (true)` when the user does something 'significant' in the app.

License
-------
Copyright 2012. [Ivan Nikitin] [ivann].
This library is distributed under the terms of the MIT/X11.

While not required, I greatly encourage and appreciate any improvements that you make
to this library be contributed back for the benefit of all who use Appirater.

[homepage]: http://arashpayan.com/blog/index.php/2009/09/07/presenting-appirater/
[xamarin]: https://github.com/xamarin/monotouch-samples/blob/master/ReachabilitySample/reachability.cs
[ivann]: https://www.visualwatermark.com
