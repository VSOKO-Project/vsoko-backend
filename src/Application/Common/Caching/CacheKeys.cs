using Application.Common.Periods;

namespace Application.Common.Caching;

public static class CacheKeys
{
    public static class Workload
    {
        public static string ListTag => "workload-list";
        public static string GetById(string id) => $"workload:id:{id}";
        public static string GetByIdForStudent(string id, string groupId, string userId) => $"workload:id:{id}:group:{groupId}:student:{userId}";
        public static string GetPaged(int page, int pageSize, string query, string period) => $"workload:list:p{page}:s{pageSize}:per{period}:q{query}";
        public static string GetPagedForStudent(int page, int pageSize, string groupId, string userId, string query, string period) => $"workload:list:p{page}:s{pageSize}:group:{groupId}:student:{userId}:per{period}:q{query}";
    }

    public static class Criteria
    {
        public static string ListTag => "criteria-list";
        public const string All = "criteria:all";
        public static string GetById(string id) => $"criteria:id:{id}";
    }

    public static class Discipline
    {
        public static string ListTag => "discipline-list";
        public static string GetPaged(int page, int pageSize, string query) => $"discipline:list:p{page}:s{pageSize}:q{query}";
        public static string GetRating(int page, int pageSize, string query, PeriodFilter period) => $"discipline:rating:p{page}:s{pageSize}:{period.CacheKey}:q{query}";
        public static string GetSummary(string id, PeriodFilter period) => $"discipline:summary:{id}:{period.CacheKey}";
    }

    public static class Feedback
    {
        public static string ListTag(string studentId) => $"feedback-list:student:{studentId}";
        public static string GetByStudent(string userId) => $"feedback:student:{userId}";
        public static string GetById(string id, string studentId) => $"feedback:id:{id}:student:{studentId}";
        public static string GetPaged(int page, int pageSize, string? disciplineId = null, string? teacherId = null, string? workloadId = null) => $"feedback:list:p{page}:s{pageSize}:d{disciplineId}:t{teacherId}:w{workloadId}";
        public static string GetPagedForStudent(int page, int pageSize, string studentId, string? disciplineId = null, string? teacherId = null, string? workloadId = null) => $"feedback:list:p{page}:s{pageSize}:student:{studentId}:d{disciplineId}:t{teacherId}:w{workloadId}";
    }

    public static class Teacher
    {
        public static string ListTag => "teacher-list";
        public static string GetPaged(int page, int pageSize, string query) => $"teacher:list:p{page}:s{pageSize}:q{query}";
        public static string GetRating(int page, int pageSize, string query, PeriodFilter period) => $"teacher:rating:p{page}:s{pageSize}:{period.CacheKey}:q{query}";
        public static string GetSummary(string id, PeriodFilter period) => $"teacher:summary:{id}:{period.CacheKey}";
    }
}