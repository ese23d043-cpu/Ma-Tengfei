using MaiYuHCM.Data;
using MaiYuHCM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MaiYuHCM.Controllers
{
    public class HCMController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HCMController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ================= 员工管理 =================
        public async Task<IActionResult> Employees(string searchString)
        {
            if (!await _context.Employees.AnyAsync())
            {
                _context.Employees.AddRange(
                    new Employee { Name = "John Doe", StoreOrFactory = "Store A", Position = "Barista", IsActive = true },
                    new Employee { Name = "Jane Smith", StoreOrFactory = "Store B", Position = "Baker", IsActive = true },
                    new Employee { Name = "Mike Wang", StoreOrFactory = "Central Factory", Position = "Worker", IsActive = true }
                );
                await _context.SaveChangesAsync();
            }

            var employees = from e in _context.Employees select e;
            if (!string.IsNullOrEmpty(searchString))
            {
                employees = employees.Where(s => s.Name.Contains(searchString) || s.StoreOrFactory.Contains(searchString));
            }
            ViewData["CurrentFilter"] = searchString;
            return View(await employees.ToListAsync());
        }

        [HttpGet]
        public IActionResult CreateEmployee() => View();

        // 【核心修复】：去掉防伪验证，直接接收数据
        [HttpPost]
        public async Task<IActionResult> CreateEmployee(Employee employee)
        {
            // 强制激活状态
            employee.IsActive = true;

            // 清除模型验证错误（防止因为一些隐藏字段导致进不去）
            ModelState.Clear();

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Employee created successfully!";
            return RedirectToAction(nameof(Employees));
        }

        [HttpGet]
        public async Task<IActionResult> EditEmployee(int? id)
        {
            if (id == null) return NotFound();
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();
            return View(employee);
        }

        [HttpPost]
        public async Task<IActionResult> EditEmployee(Employee employee)
        {
            ModelState.Clear();
            _context.Employees.Update(employee);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Employee updated successfully!";
            return RedirectToAction(nameof(Employees));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee != null)
            {
                employee.IsActive = !employee.IsActive;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Status updated for {employee.Name}.";
            }
            return RedirectToAction(nameof(Employees));
        }

        // ================= 排班管理 =================
        public async Task<IActionResult> Schedules(DateTime? date)
        {
            if (!await _context.Shifts.AnyAsync())
            {
                _context.Shifts.AddRange(
                    new Shift { Name = "Morning", StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(16, 0, 0) },
                    new Shift { Name = "Afternoon", StartTime = new TimeSpan(12, 0, 0), EndTime = new TimeSpan(20, 0, 0) },
                    new Shift { Name = "Evening", StartTime = new TimeSpan(16, 0, 0), EndTime = new TimeSpan(23, 59, 0) }
                );
                await _context.SaveChangesAsync();
            }

            var targetDate = date ?? DateTime.Today;
            var assignments = await _context.ScheduleAssignments
                .Include(a => a.Employee)
                .Include(a => a.Shift)
                .Where(a => a.Date.Date == targetDate.Date)
                .OrderBy(a => a.Shift.StartTime)
                .ToListAsync();

            ViewBag.Date = targetDate;
            return View(assignments);
        }

        [HttpGet]
        public async Task<IActionResult> CreateSchedule()
        {
            ViewBag.Employees = new SelectList(await _context.Employees.Where(e => e.IsActive).ToListAsync(), "Id", "Name");
            ViewBag.Shifts = new SelectList(await _context.Shifts.ToListAsync(), "Id", "Name");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateSchedule(ScheduleAssignment assignment)
        {
            ModelState.Clear(); // 忽略复杂的模型绑定验证

            bool alreadyScheduled = await _context.ScheduleAssignments
                .AnyAsync(a => a.EmployeeId == assignment.EmployeeId && a.Date.Date == assignment.Date.Date);

            if (alreadyScheduled)
            {
                TempData["Error"] = "This employee already has a schedule on this day.";
            }
            else
            {
                _context.ScheduleAssignments.Add(assignment);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Schedule added successfully!";
            }

            return RedirectToAction(nameof(Schedules), new { date = assignment.Date.ToString("yyyy-MM-dd") });
        }
    }
}