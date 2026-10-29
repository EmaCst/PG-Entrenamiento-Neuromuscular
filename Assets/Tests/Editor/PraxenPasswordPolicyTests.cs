using NUnit.Framework;

public class PraxenPasswordPolicyTests
{
    [Test] public void ValidPersonalPasswordIsAccepted()
    {Assert.IsNull(PraxenPasswordPolicy.Error("praxen01","MiClaveNueva2026","MiClaveNueva2026"));}
    [TestCase(null,"Personal123","Personal123")]
    [TestCase("temporary","short","short")]
    [TestCase("temporary","temporary","temporary")]
    [TestCase("temporary","praxen01","praxen01")]
    [TestCase("temporary","Personal123","OtraClave")]
    public void InvalidChangeIsRejected(string current,string next,string confirmation)
    {Assert.IsNotNull(PraxenPasswordPolicy.Error(current,next,confirmation));}
    [Test] public void Utf8ByteLimitMatchesBcryptServerPolicy()
    {
        string valid=new string('á',36),invalid=new string('á',37);
        Assert.IsNull(PraxenPasswordPolicy.Error("temporary",valid,valid));
        Assert.IsNotNull(PraxenPasswordPolicy.Error("temporary",invalid,invalid));
    }
    [Test] public void AsciiByteLimitIs72()
    {
        string valid=new string('a',72),invalid=new string('a',73);
        Assert.IsNull(PraxenPasswordPolicy.Error("temporary",valid,valid));
        Assert.IsNotNull(PraxenPasswordPolicy.Error("temporary",invalid,invalid));
    }
}
