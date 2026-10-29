using NUnit.Framework;

public class PraxenSceneRoutesTests
{
    [TestCase("TelefonoManos","GafasManos")]
    [TestCase("TelefonoPies","GafasPies")]
    [TestCase("TelefonoCorrer","GafasCorrer")]
    public void EachExerciseHasSeparateGlassesAndSelfieScenes(string canonical,string glasses)
    {
        Assert.AreEqual(glasses,PraxenSceneRoutes.Resolve(canonical,true));
        Assert.AreEqual(canonical,PraxenSceneRoutes.Resolve(canonical,false));
        Assert.AreEqual(canonical,PraxenSceneRoutes.Canonical(glasses));
        Assert.AreEqual(canonical,PraxenSceneRoutes.Resolve(glasses,false));
        Assert.AreEqual(glasses,PraxenSceneRoutes.Resolve(glasses,true));
        Assert.IsTrue(PraxenSceneRoutes.IsExercise(glasses));
        Assert.IsTrue(PraxenSceneRoutes.IsExercise(canonical));
        Assert.IsTrue(PraxenSceneRoutes.IsGlasses(glasses));
        Assert.IsFalse(PraxenSceneRoutes.IsGlasses(canonical));
    }
    [TestCase("MenuTelefono")][TestCase("CuentaRegresiva")][TestCase("SesionCombinada")]
    public void SharedScreensAreNotRemapped(string scene)
    { Assert.AreEqual(scene,PraxenSceneRoutes.Resolve(scene,true));Assert.IsFalse(PraxenSceneRoutes.IsGlasses(scene)); }
    [Test]
    public void CircuitKeepsSetsDurationsAndDisabledStagesInBothModes()
    {
        var stages=TrainingPlan.Circuit(2,15,0,20);
        Assert.AreEqual(4,stages.Count);
        foreach(var stage in stages)
        {
            Assert.AreNotEqual("TelefonoPies",stage.Scene);
            Assert.IsTrue(PraxenSceneRoutes.IsGlasses(PraxenSceneRoutes.Resolve(stage.Scene,true)));
            Assert.AreEqual(stage.Scene,PraxenSceneRoutes.Resolve(stage.Scene,false));
        }
        Assert.AreEqual(2,stages[3].Set);Assert.AreEqual(20,stages[3].Seconds);
    }
    [TestCase("TelefonoManos",true)] [TestCase("TelefonoPies",true)] [TestCase("TelefonoCorrer",true)]
    [TestCase("GafasManos",true)] [TestCase("GafasPies",true)] [TestCase("GafasCorrer",true)]
    [TestCase("SesionCombinada",true)] [TestCase("CuentaRegresiva",true)]
    [TestCase("MenuTelefono",false)] [TestCase("",false)] [TestCase("Other",false)]
    public void ScreenStaysAwakeOnlyInTrainingScenes(string scene,bool expected)
    {Assert.AreEqual(expected,PraxenSceneRoutes.KeepsDisplayAwake(scene));}
}

