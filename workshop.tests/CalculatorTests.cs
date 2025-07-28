using workshop.calculator;

namespace workshop.tests;

public class CalculatorTests
{
   

    [Test]
    public void Add_OnePlusTwo_ReturnsThree()
    {
        Calculator calculator = new Calculator();
        int a = 1;
        int b = 2;
        int result = calculator.Add(a, b);
        Assert.That(result, Is.EqualTo(3));
    }
    [TestCase(new int[] { 1, 2, 3 }, 6)]
    [TestCase(new int[] { -1, -2, -3 }, -6)]
    [TestCase(new int[] { 0, 0, 0 }, 0)]
    [TestCase(new int[] { }, 0)] // Empty collection
    [TestCase(new int[] { 10 }, 10)]
    [TestCase(new int[] { 1,1,1,1,1,1 }, 6)]
    public void Add_WithVariousInputs_ReturnsExpectedSum(int[] input, int expected)
    {
        Calculator calculator = new Calculator();
        // Act
        int result = calculator.Add(input);

        // Assert
        Assert.That(expected, Is.EqualTo(result));
    }
    [TestCase(1,1,2)]
    [TestCase(2, 3, 5)]
    [TestCase(12, 13, 25)]
    [TestCase(12, 12, 24)]
    [TestCase(5, 5, 10)]
    [TestCase(9, 9, 18)]
    public void TestCalculatorAddMethod(int a, int b, int expected)
    {
        Calculator calculator = new Calculator();
        int result = calculator.Add(a, b);
        Assert.That(expected, Is.EqualTo(result));
    }   
}
