using Newtonsoft.Json;
using System;

namespace SmartHomeWeb.Model
{
	/// <summary>
	/// A data structure that contains a person's data, 
	/// but does not capture their unique identifier.
	/// </summary>
	public sealed class PersonData
	{
        // Empty constructor is required for serialization.
        private PersonData()
        { }

        public PersonData(
            string UserName, string Password, string Name, 
            DateTime Birthdate, string Address, string City, 
            string ZipCode)
		{
            this.UserName = UserName;
            this.Password = Password;
			this.Name = Name;
            this.Birthdate = Birthdate;
            this.Address = Address;
            this.City = City;
            this.ZipCode = ZipCode;
		}

        /// <summary>
        /// Gets the person's username.
        /// </summary>
        [JsonProperty("username", Required = Required.Always)]
        public string UserName { get; private set; }

        /// <summary>
        /// Gets the person's password.
        /// </summary>
        /// <value>The password.</value>
        /// <remarks>Obviously not plaintext</remarks>
        [JsonProperty("password", Required = Required.Always)]
        public string Password { get; private set; }

        /// <summary>
        /// Gets the person's name.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; private set; }

        /// <summary>
        /// Gets the person's birthdate.
        /// </summary>
        [JsonProperty("birthdate")]
        public DateTime Birthdate { get; private set; }

        /// <summary>
        /// Gets the address.
        /// </summary>
        /// <value>The address.</value>
        [JsonProperty("address")]
        public string Address { get; private set; }

        /// <summary>
        /// Gets the name of the city that the person lives in.
        /// </summary>
        [JsonProperty("city")]
        public string City { get; private set; }

        /// <summary>
        /// Gets the zip code of the person's area of residence.
        /// </summary>
        /// <value>The zip code.</value>
        /// <remarks>Not all countries have number-only zip codes.</remarks>
        [JsonProperty("zipcode")]
        public string ZipCode { get; private set; }
	}
}

