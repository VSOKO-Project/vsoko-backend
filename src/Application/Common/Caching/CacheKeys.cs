using Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace Application.Common.Caching;

public static class CacheKeys
{
    public static class Workload
    {
        public static string ListTag => "workload-list";
        public static string GetById(string id) => $"workload:id:{id}";
        public static string GetByIdForStudent(string id, string groupId) => $"workload:id:{id}:group:{groupId}";
        public static string GetPaged(int page, int pageSize) => $"workload:list:p{page}:s{pageSize}";
        public static string GetPagedForStudent(int page, int pageSize, string groupId) => $"workload:list:p{page}:s{pageSize}:group:{groupId}";
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
        public const string All = "discipline:all";
    }

    public static class Feedback
    {
        public static string ListTag(string studentId) => $"feedback-list:student:{studentId}";
        public static string GetByStudent(string userId) => $"feedback:student:{userId}";
        public static string GetById(string id, string studentId) => $"feedback:id:{id}:student:{studentId}";
    }

    public static class Teacher
    {
        public static string ListTag => "teacher-list";
        public const string All = "teacher:all";
    }
}