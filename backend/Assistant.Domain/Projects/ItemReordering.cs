﻿using FluentResults;

namespace Assistant.Domain.Projects;

public record ItemReordering<TProject, TMeta, TItem>(string ItemId, string? PrecedingItemId = null) : IChange<TProject> where TProject : Project<TMeta, TItem> where TItem : ProjectItem
{
    public Result ApplyTo(TProject project)
    {
        var item = project.Items.SingleOrDefault(a => a.Id == ItemId);
        if (item is null) return Result.Fail("Item to reorder was not found");

        project.Items.Remove(item);
        var change = new ItemAddition<Project<TMeta, TItem>, TMeta, TItem>(item, PrecedingItemId);
        return change.ApplyTo(project);
    }

    public string Description(TProject project)
    {
        var activity = project.Items.Single(a => a.Id == ItemId);
        var precedingActivity = PrecedingItemId is null
            ? null
            : project.Items.SingleOrDefault(a => a.Id == PrecedingItemId);

        return precedingActivity is null
            ? $"Move \"{activity.Name}\" to end"
            : $"Move \"{activity.Name}\" to after \"{precedingActivity.Name}\"";
    }
}