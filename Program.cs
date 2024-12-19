using System.Diagnostics;
using System.Resources;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StepWise.Core;
using StepWise.WebAPI;

var host = Host.CreateDefaultBuilder()
    .UseStepWiseServer()
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder.UseUrls("http://localhost:5123");
    })
    .Build();

await host.StartAsync();

var client = host.Services.GetRequiredService<StepWiseClient>();
client.AddWorkflow(Workflow.CreateFromInstance(new README()));
client.AddWorkflow(Workflow.CreateFromInstance(new AutoMobileResxGenerator()));
client.AddWorkflow(Workflow.CreateFromInstance(new ProjectManager()));

await host.WaitForShutdownAsync();

public class README
{
    [Step(description: """
    # The generator for .resx files
    Generate huge, complex, and nested .resx files with ease.
    """)]
    public async Task Start()
    {
    }
}

public class AutoMobileResxGenerator
{
    [StepWiseUINumberInput(description: "The number of resource file to generate.")]
    public async Task<double?> NumberOfFiles()
    {
        return 1;
    }

    [StepWiseUITextInput(description: "The name prefix of the resource file.")]
    public async Task<string?> NamePrefix()
    {
        return "AutoMobile";
    }

    [StepWiseUINumberInput(description: "The number of cars in each resource file.")]
    public async Task<double?> NumberOfCars()
    {
        return 1;
    }

    [Step(description: "Generate the resource files.")]
    [DependOn(nameof(NumberOfFiles))]
    [DependOn(nameof(NamePrefix))]
    public async Task<string> GenerateResourceFiles(
        [FromStep(nameof(NumberOfFiles))] double numberOfFiles,
        [FromStep(nameof(NumberOfCars))] double numberOfCars,
        [FromStep(nameof(NamePrefix))] string namePrefix)
    {
        for (int i = 0; i < numberOfFiles; i++)
        {
            var fileName = $"{namePrefix}-{i}.resx";
            using var resx = new ResXResourceWriter(fileName);
            resx.AddResource("Title", "Classic American Cars");
            resx.AddResource("HeaderString1", "Make");
            resx.AddResource("HeaderString2", "Model");
            resx.AddResource("HeaderString3", "Year");
            resx.AddResource("HeaderString4", "Doors");
            resx.AddResource("HeaderString5", "Cylinders");

            for (int j = 0; j < numberOfCars; j++)
            {
                var car = new Automobile("Ford", "Mustang", 1967, 2, 8);
                resx.AddResource($"Car{j + 1}Make", car.Make);
                resx.AddResource($"Car{j + 1}Model", car.Model);
                resx.AddResource($"Car{j + 1}Year", car.Year.ToString());
                resx.AddResource($"Car{j + 1}Doors", car.Doors.ToString());
                resx.AddResource($"Car{j + 1}Cylinders", car.Cylinders.ToString());
            }

            resx.Close();
        }

        return $"Generated {numberOfFiles} resource files with {numberOfCars} cars each in under {namePrefix}-*.resx.";
    }
}

/// <summary>
/// Manage adding/removing projects to the resx-generator.sln
/// </summary>
public class ProjectManager
{
    private const string SolutionFile = "resx-generator.sln";
    private const string ThisProjectFile = "resx-generator.csproj";

    [Step(description: "list all projects in the solution.")]
    public async Task<string> ListProjects()
    {
        var solution = SolutionFile;
        var command = $"dotnet sln {solution} list";
        var output = RunProcess(command);

        var projects = output.Split(Environment.NewLine).Where(x => x.EndsWith(".csproj") && !x.Contains("resx-generator"));

        if (projects.Count() == 0)
        {
            return "No projects found in the solution.";
        }

        return string.Join(Environment.NewLine, projects);
    }

    [Step(description: "Anti-Thanos snap: randomly increase 50% of the projects in the solution.")]
    [DependOn(nameof(ListProjects))]
    public async Task<string> AntiThanosSnap(
        [FromStep(nameof(ListProjects))] string projects)
    {
        var projectsList = projects.Split(Environment.NewLine).Where(x => x.EndsWith(".csproj")).ToList();

        var projectToAdd = projectsList.Count / 2;
        if (projectToAdd == 0)
        {
            projectToAdd = 1;
        }

        var sb = new StringBuilder();
        for (int i = 0; i < projectToAdd; i++)
        {
            var projectName = GenerateRandomProjectName();
            var projectFile = $"{projectName}.csproj";
            var newProjectCommand = $"dotnet new console -n {projectName}";
            RunProcess(newProjectCommand);
            var command = $"dotnet sln {SolutionFile} add {projectName}";
            var output = RunProcess(command);
            sb.AppendLine(output);
        }

        return $"Added {projectToAdd} projects to the solution." + Environment.NewLine + sb.ToString();
    }

    [Step(description: "Thanos snap: randomly remove 50% of the projects in the solution.")]
    [DependOn(nameof(ListProjects))]
    public async Task<string> ThanosSnap(
        [FromStep(nameof(ListProjects))] string projects)
    {
        var projectsList = projects.Split(Environment.NewLine).Where(x => x.EndsWith(".csproj")).ToList();

        var projectToRemove = projectsList.Count / 2;
        if (projectToRemove == 0)
        {
            projectToRemove = 1;
        }

        for (int i = 0; i < projectToRemove; i++)
        {
            var projectIndex = new Random().Next(projectsList.Count);
            var projectFile = projectsList[projectIndex];

            if (projectFile.Contains(ThisProjectFile))
            {
                continue;
            }
            var command = $"dotnet sln {SolutionFile} remove {projectFile}";
            RunProcess(command);

            var removeFolderCommand = $"rmdir /s /q {projectFile.Replace(".csproj", "")}";
            RunProcess(removeFolderCommand);
        }

        return $"Removed {projectToRemove} projects from the solution.";
    }

    [Step(description: "Move and override .resx files to every project in the solution.")]
    [DependOn(nameof(ListProjects))]
    public async Task<string> MoveResxFiles(
        [FromStep(nameof(ListProjects))] string projects)
    {
        var projectsList = projects.Split(Environment.NewLine).Where(x => x.EndsWith(".csproj")).ToList();

        var resxFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.resx");

        var sb = new StringBuilder();
        foreach (var project in projectsList)
        {
            if (project.Contains(ThisProjectFile))
            {
                continue;
            }
            
            foreach (var resxFile in resxFiles)
            {
                var command = $"copy {resxFile} {project.Replace(".csproj", "")} /y";
                var output = RunProcess(command);
                sb.AppendLine(output);
            }
        }

        return $"Moved {resxFiles.Length} .resx files to every project in the solution." + Environment.NewLine + sb.ToString();
    }

    private string GenerateRandomProjectName()
    {
        var random = new Random();
        string[] colors = [
            "Red", "Blue", "Green", "Yellow", "Black", "White", "Orange", "Purple", "Pink", "Brown"
        ];

        string[] animals = [
            "Dog", "Cat", "Bird", "Fish", "Lion", "Tiger", "Bear", "Elephant", "Monkey", "Giraffe"
        ];

        string[] mood = [
            "Happy", "Sad", "Angry", "Excited", "Bored", "Tired", "Hungry", "Thirsty", "Sick", "Healthy"
        ];

        return $"{colors[random.Next(colors.Length)]}{animals[random.Next(animals.Length)]}{mood[random.Next(mood.Length)]}";
    }

    private string RunProcess(string command)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        process.StandardInput.WriteLine(command);
        process.StandardInput.Flush();
        process.StandardInput.Close();
        process.WaitForExit();

        return process.StandardOutput.ReadToEnd();
    }
}