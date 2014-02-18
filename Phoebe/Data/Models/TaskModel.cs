using System;
using System.Linq.Expressions;
using Newtonsoft.Json;

namespace Toggl.Phoebe.Data.Models
{
    public class TaskModel : Model
    {
        private static string GetPropertyName<T> (Expression<Func<TaskModel, T>> expr)
        {
            return expr.ToPropertyName ();
        }

        private readonly int workspaceRelationId;
        private readonly int projectRelationId;

        public TaskModel ()
        {
            workspaceRelationId = ForeignRelation<WorkspaceModel> (PropertyWorkspaceId, PropertyWorkspace);
            projectRelationId = ForeignRelation<ProjectModel> (PropertyProjectId, PropertyProject);
        }

        protected override void Validate (ValidationContext ctx)
        {
            base.Validate (ctx);

            if (ctx.HasChanged (PropertyName)) {
                if (String.IsNullOrWhiteSpace (Name)) {
                    ctx.AddError (PropertyName, "Task name cannot be empty.");
                } else if (Model.Query<TaskModel> (
                               (m) => m.Name == Name
                               && m.ProjectId == ProjectId
                               && m.Id != Id
                           ).NotDeleted ().Count () > 0) {
                    ctx.AddError (PropertyName, "Task with such name already exists.");
                }
            }

            if (ctx.HasChanged (PropertyProjectId)
                || ctx.HasChanged (PropertyWorkspaceId)) {

                ctx.ClearErrors (PropertyProjectId);
                ctx.ClearErrors (PropertyProject);
                ctx.ClearErrors (PropertyWorkspaceId);

                if (ProjectId == null) {
                    ctx.AddError (PropertyProjectId, "Task must be associated with a project.");
                } else if (Project == null) {
                    ctx.AddError (PropertyProject, "Associated project could not be found.");
                }

                if (Project != null && Project.WorkspaceId != WorkspaceId) {
                    ctx.AddError (PropertyWorkspaceId, "Task workspace doesn't match the projects workspace.");
                }
            }
        }

        #region Data

        private string name;
        public static readonly string PropertyName = GetPropertyName ((m) => m.Name);

        [JsonProperty ("name")]
        public string Name {
            get {
                lock (SyncRoot) {
                    return name;
                }
            }
            set {
                lock (SyncRoot) {
                    if (name == value)
                        return;

                    ChangePropertyAndNotify (PropertyName, delegate {
                        name = value;
                    });
                }
            }
        }

        private bool active;
        public static readonly string PropertyIsActive = GetPropertyName ((m) => m.IsActive);

        [JsonProperty ("active")]
        public bool IsActive {
            get {
                lock (SyncRoot) {
                    return active;
                }
            }
            set {
                lock (SyncRoot) {
                    if (active == value)
                        return;

                    ChangePropertyAndNotify (PropertyIsActive, delegate {
                        active = value;
                    });
                }
            }
        }

        private long estimate;
        public static readonly string PropertyEstimate = GetPropertyName ((m) => m.Estimate);

        [JsonProperty ("estimated_seconds")]
        public long Estimate {
            get {
                lock (SyncRoot) {
                    return estimate;
                }
            }
            set {
                lock (SyncRoot) {
                    if (estimate == value)
                        return;

                    ChangePropertyAndNotify (PropertyEstimate, delegate {
                        estimate = value;
                    });
                }
            }
        }

        #endregion

        #region Relations

        public static readonly string PropertyWorkspaceId = GetPropertyName ((m) => m.WorkspaceId);

        public Guid? WorkspaceId {
            get { return GetForeignId (workspaceRelationId); }
            set { SetForeignId (workspaceRelationId, value); }
        }

        public static readonly string PropertyWorkspace = GetPropertyName ((m) => m.Workspace);

        [DontDirty]
        [SQLite.Ignore]
        [JsonProperty ("wid"), JsonConverter (typeof(ForeignKeyJsonConverter))]
        public WorkspaceModel Workspace {
            get { return GetForeignModel<WorkspaceModel> (workspaceRelationId); }
            set { SetForeignModel (workspaceRelationId, value); }
        }

        public static readonly string PropertyProjectId = GetPropertyName ((m) => m.ProjectId);

        public Guid? ProjectId {
            get { return GetForeignId (projectRelationId); }
            set { SetForeignId (projectRelationId, value); }
        }

        public static readonly string PropertyProject = GetPropertyName ((m) => m.Project);

        [DontDirty]
        [SQLite.Ignore]
        [JsonProperty ("pid"), JsonConverter (typeof(ForeignKeyJsonConverter))]
        public ProjectModel Project {
            get { return GetForeignModel<ProjectModel> (projectRelationId); }
            set { SetForeignModel (projectRelationId, value); }
        }

        public IModelQuery<TimeEntryModel> TimeEntries {
            get { return Model.Query<TimeEntryModel> ((m) => m.TaskId == Id); }
        }

        #endregion
    }
}
