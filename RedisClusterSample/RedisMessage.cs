public class RedisMessage
{
    public RedisDataType DataType { get; set; }
    public string Key { get; set; }
    public string Value { get; set; }
}
public enum RedisDataType
{
    Organization,
    Password,
    Product
}