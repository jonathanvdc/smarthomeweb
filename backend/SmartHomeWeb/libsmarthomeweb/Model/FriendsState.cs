using System;

namespace SmartHomeWeb
{
	/// <summary>
	/// Describes possible states for a 'friends' relationship.
	/// </summary>
	public enum FriendsState
	{
		/// <summary>
		/// Persons one and two are not friends.
		/// </summary>
		None = 0,
		/// <summary>
		/// Person one has sent a friend request to person two.
		/// </summary>
		FriendRequestSent = 1,
		/// <summary>
		/// Person two has sent a friend request to person one.
		/// </summary>
		FriendRequestRecieved = 2,
		/// <summary>
		/// Persons one and two are friends.
		/// </summary>
		Friends = FriendRequestSent | FriendRequestRecieved
	}
}

