Using Carrot with Unity                         {#mainpage}
============

# Carrot User Authentication

Carrot user authentication using Facebook SSO can be done directly through the C# API.

	if (Carrot.Instance.doFacebookAuth()) {
		// Facebook SSO has started
	} else {
		// Facebook SSO has not started
	}

# Carrot API Calls

When you make Carrot API calls they will be cached on the device using SQLite. This means that your user's actions will not be lost if device connectivity is lost. It will also store up the actions before a user has authenticated so that if they authenticate later, none of their achievements or high scores are lost.

	// To post an achievement with the 'chicken' identifier:
	Carrot.Instance.postAchievement("chicken");

	// To post a high score:
	Carrot.Instance.postHighScore(42);

	// To post an Open Graph Action with a dynamically created Open Graph Object:
	IDictionary objectProperties = new Dictionary<string, object>();
	objectProperties.Add("title", "Unity Test");
	objectProperties.Add("image", "http://static.ak.fbcdn.net/rsrc.php/v2/y_/r/9myDd8iyu0B.gif");
	objectProperties.Add("description", "Testing the Unity dynamic object generation");
	Carrot.Instance.postAction("push", "commit", objectProperties);

	// To send a 'Like' for your game:
	Carrot.Instance.likeGame();

For more information please see [Getting Started with Carrot for Unity](https://gocarrot.com/docs/unity).
