using Newtonsoft.Json;

namespace RemoteAgent.Common.Serialization
{
	public class JsonSerializedType<T>
	{
		[JsonIgnore]
		public T Value
		{
			get
			{
				if (Serialized == null)
					return default(T);

				return (T) Serialized;
			}
			set { Serialized = value; }
		}

		[JsonConverter(typeof(UnknownObjectConverter))]
		public object Serialized { get; set; }

		public static implicit operator T(JsonSerializedType<T> source) => source.Value;

		public JsonSerializedType(T data)
		{
			Value = data;
		}
	}
}