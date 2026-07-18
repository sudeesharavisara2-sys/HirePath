# Resume AI - Intelligent Resume Screening & ATS Platform

![ATS Dashboard](HirePathAI/docs/screenshots/ats_home_page.png)

## Overview

Resume AI is an AI-powered Resume Screening System built with ASP.NET Core MVC, ML.NET, and Clean Architecture principles.

The application helps HR teams automate the initial candidate screening process by analyzing PDF resumes, extracting candidate information, matching candidates against job requirements, calculating ATS scores, and generating hiring recommendations.

Instead of manually reviewing hundreds of resumes, recruiters can quickly identify the most relevant candidates based on skills, experience, education, certifications, and job-specific requirements.

---

## Business Problem

Organizations often receive a large number of applications for a single position.

Manual resume screening creates several challenges:

* Time-consuming review process
* Inconsistent candidate evaluation
* Difficulty identifying qualified applicants
* High dependency on manual filtering
* Increased hiring effort

Resume AI addresses these challenges by automating candidate analysis and providing structured hiring insights.

---

## How the System Works

```text
HR Creates Job
        ↓
Candidate Uploads Resume
        ↓
PDF Text Extraction
        ↓
Resume Parsing
        ↓
Skills Extraction
        ↓
Experience Analysis
        ↓
Education Analysis
        ↓
Certification Detection
        ↓
Resume vs Job Matching
        ↓
ATS Score Calculation
        ↓
ML.NET Prediction
        ↓
Selected / Consider / Rejected
```

---

## Core Features

### Dynamic Job Management

* HR creates job requirements at runtime
* No hardcoded technology roles
* Supports multiple job categories
* Supports different companies and hiring requirements

### Resume Parsing

Extracts:

* Candidate Name
* Contact Information
* Skills
* Work Experience
* Education
* Certifications
* Projects

Supports multiple resume formats and layouts.

### Experience Analysis

* Calculates total professional experience
* Supports multiple companies
* Handles promotions and role changes
* Supports different date formats
* Detects current employment

### Job Matching Engine

Compares resumes against:

* Required Skills
* Preferred Skills
* Experience Requirements
* Education Requirements

Generates dynamic match scores based on the selected job.

### ATS Scoring Engine

Calculates candidate scores using:

* Skills Match
* Experience Match
* Education Match
* Certification Match
* ML.NET Confidence

### ML.NET Prediction

ML.NET acts as a supporting prediction layer that complements rule-based ATS scoring.

The model provides:

* Match Confidence
* Candidate Recommendation
* Additional decision support

### Candidate Ranking

Ranks candidates based on:

* ATS Score
* Match Percentage
* Job Requirements
* ML.NET Prediction

### ATS Dashboard

Provides:

* ATS Score Distribution
* Selection Ratio
* Screening Summary
* Candidate Processing Metrics
* Candidate Rankings
* Activity Monitoring

---

## Technology Stack

### Backend

* ASP.NET Core MVC
* .NET 8
* C#

### Machine Learning

* ML.NET
* Binary Classification Model

### Document Processing

* UglyToad.PdfPig

### Frontend

* HTML5
* CSS3
* JavaScript

### Visualization

* Chart.js

---

## Architecture

The solution follows Clean Architecture principles.

### Domain Layer

Contains:

* ParsedResume
* ExperienceEntry
* ResumeScore
* Business Entities

### Application Layer

Contains:

* DTOs
* Interfaces
* ATS Scoring Logic
* Business Rules

### Infrastructure Layer

Contains:

* Resume Parser
* PDF Processing
* ML.NET Prediction Services

### Presentation Layer

Contains:

* Controllers
* Razor Views
* ViewModels
* Dashboard UI

---

## Screenshots

### ATS Dashboard

![ATS Dashboard](HirePathAI/docs/screenshots/ats_home_page.png)

![Add Job](HirePathAI/docs/screenshots/add_job_screen.png)

![Job Screen](HirePathAI/docs/screenshots/job_screen.png)

### Resume Upload & Screening

![Resume Upload](HirePathAI/docs/screenshots/ats_upload_screen.png)

### Candidate Analysis Result

![Analysis Result](HirePathAI/docs/screenshots/ats_senior_analysis.png)

### Real-Time Processing Workflow

![Processing Workflow](HirePathAI/docs/screenshots/ats_processing_overlay.png)

---

## Key Business Benefits

* Reduces manual resume screening effort
* Improves hiring consistency
* Standardizes candidate evaluation
* Supports multiple job roles
* Provides explainable ATS scoring
* Accelerates recruiter decision-making

---

## Installation

Restore dependencies:

```bash
dotnet restore
```

Build the solution:

```bash
dotnet build
```

---

## Run the Application

Run the web application:

```bash
cd HirePathAI
dotnet run
```

Open:

```text
http://localhost:5059
```

---

## ML Model Training

To retrain the ML.NET model:

```bash
cd ResumeTrainer
dotnet run
```

This generates an updated model used by the screening engine.

---




