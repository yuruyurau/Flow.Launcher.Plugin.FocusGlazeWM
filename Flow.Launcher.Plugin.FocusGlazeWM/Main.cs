using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using Flow.Launcher.Plugin;

namespace Flow.Launcher.Plugin.FocusGlazeWM;

public class FocusGlazeWM : IPlugin
{
    private PluginInitContext _context = null!;

    public void Init(PluginInitContext context) => _context = context;

    public List<Result> Query(Query query)
    {
        var results = new List<Result>();

        try
        {
            var (windows, currentWorkspace) = GetAllWindowsWithWorkspace();
            var search = query.Search?.Trim() ?? "";

            foreach (var w in windows)
            {
                var processName = w.ProcessName.ToLowerInvariant();
                string prefix;

                if (w.HasFocus)
                    prefix = "*";
                else if (w.Workspace == currentWorkspace)
                    prefix = "~";
                else
                    prefix = w.Workspace;

                var title = $"{prefix}: {processName}";

                var searchTarget = $"{title} {w.Title}";

                if (string.IsNullOrEmpty(search))
                {
                    results.Add(CreateResult(title, w, null));
                    continue;
                }

                var match = _context.API.FuzzySearch(search, searchTarget);
                if (match == null || !match.Success)
                    continue;

                results.Add(CreateResult(title, w, match));
            }
        }
        catch (Exception ex)
        {
            results.Add(new Result
            {
                Title = "GlazeWM Error",
                SubTitle = ex.Message,
                IcoPath = "assets\\icon.png",
                Action = _ => false
            });
        }

        return results.OrderByDescending(r => r.Score).ToList();
    }

    private Result CreateResult(string title, GlazeWindow w, dynamic? match)
    {
        return new Result
        {
            Title = title,
            SubTitle = "",
            IcoPath = "assets\\app.png",
            Score = match?.Score ?? (w.HasFocus ? 100 : 50),
            TitleHighlightData = match?.MatchData,
            Action = _ =>
            {
                RunGlaze($"command focus --workspace {w.Workspace}");
                RunGlaze($"command focus --container-id {w.Id}");
                return true;
            }
        };
    }

    private (List<GlazeWindow> Windows, string CurrentWorkspace) GetAllWindowsWithWorkspace()
    {
        var list = new List<GlazeWindow>();
        string currentWorkspace = "";
        var json = RunGlaze("query workspaces");

        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.TryGetProperty("data", out var data) &&
            data.TryGetProperty("workspaces", out var workspaces))
        {
            foreach (var ws in workspaces.EnumerateArray())
            {
                var wsName = ws.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                bool hasFocus = ws.TryGetProperty("hasFocus", out var hf) && hf.GetBoolean();

                if (hasFocus)
                    currentWorkspace = wsName;

                CollectWindows(ws, wsName, list);
            }
        }

        return (list, currentWorkspace);
    }

    private static void CollectWindows(JsonElement element, string workspaceName, List<GlazeWindow> list)
    {
        if (element.TryGetProperty("type", out var type) && type.GetString() == "window")
        {
            list.Add(new GlazeWindow(
                element.GetProperty("id").GetString()!,
                element.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "",
                element.TryGetProperty("processName", out var p) ? p.GetString() ?? "" : "",
                workspaceName,
                element.TryGetProperty("hasFocus", out var hf) && hf.GetBoolean()
            ));
            return;
        }

        if (element.TryGetProperty("children", out var children))
        {
            foreach (var child in children.EnumerateArray())
                CollectWindows(child, workspaceName, list);
        }
    }

    private static string RunGlaze(string args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "glazewm",
            Arguments = args,
            RedirectStandardOutput = true,
            StandardOutputEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var p = Process.Start(psi)!;
        var output = p.StandardOutput.ReadToEnd();
        p.WaitForExit(3000);
        return output;
    }

    private record GlazeWindow(string Id, string Title, string ProcessName, string Workspace, bool HasFocus);
}
