using QuanLyDiemSinhVien.Models.ViewModel;

namespace QuanLyDiemSinhVien.Services
{
    public class GradeCalculator
    {
        public static double ToScale4(double s) =>
            s >= 8.5 ? 4.0 :
            s >= 8.0 ? 3.5 :
            s >= 7.0 ? 3.0 :
            s >= 6.5 ? 2.5 :
            s >= 5.5 ? 2.0 :
            s >= 5.0 ? 1.5 :
            s >= 4.0 ? 1.0 : 0.0;

        public static bool IsPassed(double s) => s >= 4.0;

        private static int SemesterOrder(string hk)
        {
            var k = (hk ?? "").Replace(" ", "").ToUpper();
            return k switch
            {
                "HK1" => 1,
                "HK2" => 2,
                "HK2.1" => 2,
                "HK2.2" => 3,
                _ => 9
            };
        }

        // Trung bình có trọng số: (gpa10, gpa4)
        private static (double, double) Weighted(IEnumerable<GradeRecord> list)
        {
            var arr = list.ToList();
            int totalCredits = arr.Sum(x => x.Credits);
            if (totalCredits == 0) return (0, 0);

            double g10 = arr.Sum(x => x.TotalScore * x.Credits) / totalCredits;
            double g4 = arr.Sum(x => ToScale4(x.TotalScore) * x.Credits) / totalCredits;
            return (Math.Round(g10, 2), Math.Round(g4, 2));
        }

        public static List<SemesterSummaryRow> BuildSummary(IEnumerable<GradeRecord> records)
        {
            var all = records.ToList();

            // Danh sách học kỳ đã sắp xếp theo thời gian
            var terms = all
                .Select(r => new { r.AcademicYear, r.Semester })
                .Distinct()
                .OrderBy(t => t.AcademicYear)
                .ThenBy(t => SemesterOrder(t.Semester))
                .ToList();

            var termIndex = terms
                .Select((t, i) => new { Key = $"{t.AcademicYear}|{t.Semester}", Index = i })
                .ToDictionary(x => x.Key, x => x.Index);

            int IndexOf(GradeRecord r) => termIndex[$"{r.AcademicYear}|{r.Semester}"];

            var rows = new List<SemesterSummaryRow>();

            for (int i = 0; i < terms.Count; i++)
            {
                var t = terms[i];

                // Điểm riêng học kỳ này
                var semRecords = all.Where(r => IndexOf(r) == i && r.IncludeInGraduation).ToList();
                var (sem10, sem4) = Weighted(semRecords);

                // Tích lũy: các môn tới hết học kỳ i, môn học lại lấy điểm cao nhất
                var cumAll = all
                    .Where(r => IndexOf(r) <= i)
                    .GroupBy(r => r.CourseCode)
                    .Select(g => g.OrderByDescending(x => x.TotalScore).First())
                    .ToList();

                var counted = cumAll.Where(r => r.IncludeInGraduation).ToList();
                var (cum10, cum4) = Weighted(counted);

                rows.Add(new SemesterSummaryRow
                {
                    Index = i + 1,
                    SemesterName = $"{t.AcademicYear}_{t.Semester}",
                    SemGpa4 = sem4,
                    SemGpa10 = sem10,
                    CumGpa4 = cum4,
                    CumGpa10 = cum10,
                    AccumulatedCredits = counted.Where(r => IsPassed(r.TotalScore)).Sum(r => r.Credits),
                    DebtCredits = counted.Where(r => !IsPassed(r.TotalScore)).Sum(r => r.Credits),
                    RegisteredCredits = counted.Sum(r => r.Credits),
                    NotCountedCredits = cumAll.Where(r => !r.IncludeInGraduation).Sum(r => r.Credits)
                });
            }

            return rows;
        }
    }
}
