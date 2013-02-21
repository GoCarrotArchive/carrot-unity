Using Carrot with Unity                         {#mainpage}
============

# Carrot User Authentication

First you must assign a UserId to Carrot. This UserId can be anything you want, but it must be unique for each user, and should not change.

	Carrot.Instance.UserId = "some unique user id";

Next you should validate that the user has authorized your Facebook application. You can do this by passing a Facebook User Access Token for that user, or by passing the Facebook User Id for that user.

	Carrot.Instance.validateUser("User Access Token or Facebook User Id");

You will be informed of user authentication events through the `AuthenticationStatusChanged` event.

	Carrot.AuthenticationStatusChanged += (object sender, Carrot.AuthStatus status) => {
		Debug.Log(Carrot.authStatusString(status));
	};

# Carrot API Calls

	// To post an achievement with the 'chicken' identifier:
	Carrot.Instance.postAchievement("chicken");

	// To post a high score:
	Carrot.Instance.postHighScore(42);

	// To post an Open Graph action with an existing Open Graph Object instance:
	Carrot.Instance.postAction("action_id", "object_instance_id");

	// To post an Open Graph Action with a dynamically created Open Graph Object:
	IDictionary objectProperties = new Dictionary<string, object>();
	objectProperties.Add("title", "Unity Test");
	objectProperties.Add("image", "http://static.ak.fbcdn.net/rsrc.php/v2/y_/r/9myDd8iyu0B.gif");
	objectProperties.Add("description", "Testing the Unity dynamic object generation");
	Carrot.Instance.postAction("push", "commit", objectProperties);

	// To send a 'Like' for your game:
	Carrot.Instance.likeGame();

For more information please see [Getting Started with Carrot for Unity](https://gocarrot.com/docs/unity).
