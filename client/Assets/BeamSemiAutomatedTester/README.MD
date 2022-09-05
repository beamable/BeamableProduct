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

### V. Prepare tests for the runtime (build)
1. Open `BuildSettings`
2. Delete all, non test scenes
3. Add `TestMainMenu` as **FIRST SCENE** `(index 0)`
4. Add rest tests in any order

### VI. Add Title and Description to the test in TestMainMenu
1. Go under `NewTestingTool/Resources/Tests` and select `<YourTestName>_Descriptor` scriptable object.
2. Do this steps if `NewTestingTool/Resources/Tests` doesn't exist
   1. Open `Testing Editor` at `TestingTool/Testing Editor`
   2. Press `Scan` button on the top bar
   3. Go to step 1. 
3. Fill `Title` and `Description` under `Test Descriptor` (first one)
4. Add `TestMainMenu` as **FIRST SCENE** `(index 0)` to the build settings
5. Add your selected scene to the build settings
