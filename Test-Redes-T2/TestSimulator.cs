using Simulador;

namespace Test_Redes_T2;

public class TestSimulator
{
    private static readonly TestResult[] ExpectedX =
    {
    };

    private Simulator _simulator;

    [OneTimeSetUp]
    public void SetUp()
    {
        _simulator = new Simulator();
    }

    [Test]
    public void Test1()
    {
        Assert.Pass();
    }
}