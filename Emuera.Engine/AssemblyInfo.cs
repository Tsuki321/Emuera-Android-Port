using System.Runtime.CompilerServices;

// Allow the Android front-end project to access internal engine types.
[assembly: InternalsVisibleTo("Emuera.Android")]

// Allow the test project to access internal engine types.
[assembly: InternalsVisibleTo("Emuera.Tests")]
