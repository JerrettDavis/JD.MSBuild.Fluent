# TinyBDD Refactoring Progress Tracker

Track your progress as you implement TinyBDD best practices across the test suite.

**Started**: _____________  
**Target Completion**: _____________  
**Completed**: _____________

---

## 📋 Test File Checklist

### High Priority (Quick Wins - 30 min)

- [ ] **BddFluentApiTests.cs** (110 lines)
  - [ ] Add `[Feature]` attribute
  - [ ] Add `[Scenario]` attributes to all methods
  - [ ] Add tags (smoke, fluent-api, etc.)
  - [ ] Extract lambdas to named methods
  - [ ] Remove "Scenario_" prefixes
  - [ ] Tests still pass: `dotnet test --filter "FullyQualifiedName~FluentApi"`
  - Date completed: _____________

- [ ] **PackagingValidationTests.cs** (88 lines)
  - [ ] Add `[Feature]` attribute
  - [ ] Add `[Scenario]` attributes
  - [ ] Add tags
  - [ ] Extract lambdas
  - [ ] Tests still pass
  - Date completed: _____________

### Medium Priority (1-2 hours)

- [ ] **BddPackagingTests.cs** (131 lines)
  - [ ] Add `[Feature]` attribute
  - [ ] Add `[Scenario]` attributes
  - [ ] Add tags
  - [ ] Extract lambdas
  - [ ] Replace try/finally with `.Finally()`
  - [ ] Tests still pass
  - Date completed: _____________

- [ ] **BddTargetOrchestrationTests.cs** (162 lines)
  - [ ] Add `[Feature]` attribute
  - [ ] Add `[Scenario]` attributes
  - [ ] Add tags
  - [ ] Extract lambdas
  - [ ] Tests still pass
  - Date completed: _____________

- [ ] **ValidationTests.cs** (55 lines) - **Full Conversion Required**
  - [ ] Convert to `TinyBddXunitBase`
  - [ ] Add constructor with `ITestOutputHelper`
  - [ ] Add `[Feature]` attribute
  - [ ] Convert all assertions to Given/When/Then
  - [ ] Add `[Scenario]` attributes
  - [ ] Add tags
  - [ ] Tests still pass
  - Date completed: _____________

### Complex Tests (2-3 hours)

- [ ] **BddEndToEndTests.cs** (234 lines)
  - [ ] Add `[Feature]` attribute
  - [ ] Add `[Scenario]` attributes
  - [ ] Add tags
  - [ ] Extract lambdas
  - [ ] Replace try/finally with `.Finally()`
  - [ ] Tests still pass
  - Date completed: _____________

- [ ] **BddTaskInvocationTests.cs** (191 lines)
  - [ ] Add `[Feature]` attribute
  - [ ] Add `[Scenario]` attributes
  - [ ] Add tags
  - [ ] Extract lambdas
  - [ ] Tests still pass
  - Date completed: _____________

- [ ] **BddRealWorldPatternsTests.cs** (247 lines)
  - [ ] Add `[Feature]` attribute
  - [ ] Add `[Scenario]` attributes
  - [ ] Add tags
  - [ ] Extract lambdas
  - [ ] Tests still pass
  - Date completed: _____________

### Large Test Files (3-4 hours)

- [ ] **GoldenGenerationTests.cs** (150 lines)
  - [ ] Add `[Feature]` attribute
  - [ ] Add `[Scenario]` attributes
  - [ ] Add tags
  - [ ] Extract lambdas
  - [ ] Tests still pass
  - Date completed: _____________

- [ ] **GeneratorSpecTests.cs** (172 lines)
  - [ ] Add `[Feature]` attribute
  - [ ] Add `[Scenario]` attributes
  - [ ] Add tags
  - [ ] Extract lambdas
  - [ ] Tests still pass
  - Date completed: _____________

- [ ] **EfcptParityGenerationTests.cs** (275 lines)
  - [ ] Add `[Feature]` attribute
  - [ ] Add `[Scenario]` attributes
  - [ ] Add tags
  - [ ] Extract lambdas
  - [ ] Tests still pass
  - Date completed: _____________

- [ ] **EfcptCanonicalParityTests.cs** (401 lines)
  - [ ] Add `[Feature]` attribute
  - [ ] Add `[Scenario]` attributes
  - [ ] Add tags
  - [ ] Extract lambdas
  - [ ] Tests still pass
  - Date completed: _____________

- [ ] **CoverageTests.cs** (614 lines) - **Largest File**
  - [ ] Add `[Feature]` attribute
  - [ ] Add `[Scenario]` attributes
  - [ ] Add tags
  - [ ] Extract lambdas
  - [ ] Tests still pass
  - Date completed: _____________

---

## 🎯 Milestone Tracking

### Milestone 1: Attributes & Organization
- [ ] All test classes have `[Feature]` attribute (0/15)
- [ ] All test methods have `[Scenario]` attribute (0/~100)
- [ ] Tags added to categorize tests
- [ ] Class names cleaned up (remove "Bdd" prefix if desired)
- [ ] Method names cleaned up (remove "Scenario_" prefix)

**Target**: _____________  
**Completed**: _____________

### Milestone 2: Code Quality
- [ ] All lambdas extracted to named methods
- [ ] All try/finally replaced with `.Finally()`
- [ ] FluentAssertions used consistently in Then/And/But
- [ ] No compilation warnings
- [ ] All tests still pass

**Target**: _____________  
**Completed**: _____________

### Milestone 3: Advanced Features
- [ ] Feature lifecycle hooks implemented where beneficial
- [ ] Tags documented in README or CONTRIBUTING.md
- [ ] CI/CD updated to use tag filtering
- [ ] Test execution time measured and optimized

**Target**: _____________  
**Completed**: _____________

---

## 📊 Progress Summary

**Completion Rate**: _____ / 15 files (____%)

### By Priority
- High Priority: _____ / 2 (____%)
- Medium Priority: _____ / 3 (____%)
- Complex: _____ / 3 (____%)
- Large: _____ / 5 (____%)

### By Task Type
- [Feature] attributes: _____ / 15
- [Scenario] attributes: _____ / ~100
- Tags added: _____ / 15
- Lambdas extracted: Estimated _____% complete
- .Finally() conversions: _____ / ~5
- Feature lifecycle: _____ / 3 (optional)

---

## ✅ Validation Checklist

After completing each file:

- [ ] File compiles without warnings
- [ ] All tests pass: `dotnet test`
- [ ] Feature name appears in test output
- [ ] Scenario names appear in test output
- [ ] Tags work with filtering: `dotnet test --filter "Tag=smoke"`
- [ ] Code coverage unchanged or improved
- [ ] Test execution time similar or improved

---

## 📝 Notes & Issues

Use this space to track any issues, questions, or decisions:

```
Date: _____________
Issue: _____________________________________________
Resolution: _______________________________________


Date: _____________
Issue: _____________________________________________
Resolution: _______________________________________


Date: _____________
Issue: _____________________________________________
Resolution: _______________________________________

```

---

## 🎉 Completion Checklist

When all files are refactored:

- [ ] All 15 test files refactored
- [ ] All tests pass: `dotnet test`
- [ ] Code coverage report generated
- [ ] Coverage % maintained or improved
- [ ] CI/CD pipeline updated
- [ ] Documentation updated
- [ ] Team notified of new patterns
- [ ] Remove/archive old examples
- [ ] Update CONTRIBUTING.md
- [ ] Close related GitHub issues

**Final Review Date**: _____________  
**Signed Off By**: _____________

---

**Reference Documents:**
- DOGFOODING_REVIEW.md - Complete analysis
- TINYBDD_IMPLEMENTATION_GUIDE.md - Step-by-step guide
- BddFluentApiTests.Refactored.Example.cs - Working example
- TinyBDD README: C:\git\TinyBDD\README.md
