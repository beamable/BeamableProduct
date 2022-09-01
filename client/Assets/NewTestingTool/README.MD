### I. Create new test
1. Open `Testing Editor` at `TestingTool/Testing Editor`
2. Press `Create` button on the top bar
3. Enter the test name

### II. Delete test
1. Open `Testing Editor` at `TestingTool/Testing Editor`
2. Select test which should be deleted
3. Press `Delete` button on the top bar
4. Confirm deletion

### III. Scan for missing tests
1. Open `Testing Editor` at `TestingTool/Testing Editor`
2. Press `Scan` button on the top bar

### IV. Create testable script
1. Open any testing scene listed in the `Testing Editor`. Double click on the test scene name will automatically open the test scene.
2. Create a script and name it whatever you want to
3. Open the newly created script
4. Inherit from the `Testable` class
5. Create any method and as return parameter set `TestResult`
6. Add `TestRule` attribute over it

```cs
public class TestScript : Testable
{
    [TestRule(0)]
    public TestResult Test()
    {
        return TestResult.Passed;
    }
}
```