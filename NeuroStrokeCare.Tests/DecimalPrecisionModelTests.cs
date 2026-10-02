using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using NeuroStrokeCare.Data.Entities;
using NeuroStrokeCare.infrastructure.Context;
using Xunit;

namespace NeuroStrokeCare.Tests
{
    // FINAL RELEASE-CANDIDATE PASS (section 3): model-level coverage for the explicit
    // decimal precision/scale added to NeuroFlowDbContext.OnModelCreating.
    //
    // KNOWN LIMITATION (documented rather than worked around, same spirit as the note in
    // CustomWebApplicationFactory): this does NOT exercise real SQL Server column
    // truncation/rounding behavior - SQLite (used by every other test in this suite) does
    // not enforce decimal(precision, scale) at the storage engine level at all, so a test
    // that inserted an over-scale value through the normal HTTP/SQLite path would silently
    // pass regardless of what HasPrecision says. What CAN be verified without a real SQL
    // Server connection (none is available in this sandbox - see FINAL_RELEASE_CANDIDATE_REPORT.md)
    // is that the EF model itself actually carries the intended precision/scale metadata for
    // every field the spec listed, i.e. that OnModelCreating's configuration was wired up
    // correctly and will produce the expected `decimal(p, s)` column types whenever a real
    // migration is generated against SQL Server. That is what these tests assert.
    public class DecimalPrecisionModelTests
    {
        private static IModel BuildModel()
        {
            var options = new DbContextOptionsBuilder<NeuroFlowDbContext>()
                .UseSqlite("DataSource=:memory:")
                .Options;
            using var context = new NeuroFlowDbContext(options);
            return context.Model;
        }

        private static void AssertPrecision<TEntity>(IModel model, string propertyName, int expectedPrecision, int expectedScale)
        {
            var entityType = model.FindEntityType(typeof(TEntity));
            Assert.NotNull(entityType);
            var property = entityType!.FindProperty(propertyName);
            Assert.NotNull(property);
            Assert.Equal(expectedPrecision, property!.GetPrecision());
            Assert.Equal(expectedScale, property.GetScale());
        }

        [Fact]
        public void PatientWeightKg_HasExplicitPrecision()
        {
            AssertPrecision<Patient>(BuildModel(), nameof(Patient.WeightKg), 5, 2);
        }

        [Fact]
        public void AdmissionThrombolysisDoseMg_HasExplicitPrecision()
        {
            AssertPrecision<Admission>(BuildModel(), nameof(Admission.ThrombolysisDoseMg), 6, 2);
        }

        [Fact]
        public void ICHAssessmentVolumeMl_HasExplicitPrecision()
        {
            AssertPrecision<ICHAssessment>(BuildModel(), nameof(ICHAssessment.ICHVolumeMl), 6, 2);
        }

        [Theory]
        [InlineData(nameof(LabResults.GlucoseMmol), 5, 2)]
        [InlineData(nameof(LabResults.INR), 5, 2)]
        [InlineData(nameof(LabResults.PT), 6, 2)]
        [InlineData(nameof(LabResults.Platelets), 9, 2)]
        [InlineData(nameof(LabResults.Sodium), 6, 2)]
        [InlineData(nameof(LabResults.Potassium), 5, 2)]
        [InlineData(nameof(LabResults.Creatinine), 7, 2)]
        [InlineData(nameof(LabResults.Hemoglobin), 5, 2)]
        [InlineData(nameof(LabResults.LDL), 6, 2)]
        [InlineData(nameof(LabResults.HbA1c), 5, 2)]
        [InlineData(nameof(LabResults.aPTT), 6, 2)]
        [InlineData(nameof(LabResults.ALT), 8, 2)]
        [InlineData(nameof(LabResults.AST), 8, 2)]
        public void LabResultsField_HasExplicitPrecision(string propertyName, int expectedPrecision, int expectedScale)
        {
            AssertPrecision<LabResults>(BuildModel(), propertyName, expectedPrecision, expectedScale);
        }
    }
}
