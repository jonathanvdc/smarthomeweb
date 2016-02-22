using System;

namespace SmartHomeWeb
{
	/// <summary>
	/// A data structure that contains a person's data, 
	/// but does not capture their unique identifier.
	/// </summary>
	public class PersonData
	{
		public PersonData(string Name)
		{
			this.Name = Name;
		}

		/// <summary>
		/// Gets the person's name.
		/// </summary>
		public string Name { get; private set; }
	}
}

