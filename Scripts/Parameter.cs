public class Parameter
{
    private string senderKey;
    private string receiverKey;

    public Parameter(string senderKey, string receiverKey)
    {
        this.senderKey = senderKey;
        this.receiverKey = receiverKey;
    }

    public string GetSenderKey()
    {
        return senderKey;
    }

    public string GetReceiverKey()
    {
        return receiverKey;
    }
}
