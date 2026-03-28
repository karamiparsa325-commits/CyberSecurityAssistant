# 🛡️ CyberSecurity Assistant

> An advanced, self-contained desktop application built with C# and WPF for real-time malware detection, URL scanning, and AI-powered phishing analysis.

## 📸 Screenshots

Here are different views and features of the application, showcasing its modern design and comprehensive security functionalities.

![Main Application Dashboard (Email Analysis)](image_0.png)
*Figure 1: The main application window showing the results of an email/text analysis, confirming "SAFE CONTENT" with a green shield and status message.*

![Email/Text Analysis Interface](image_2.png)
*Figure 2: A close-up view of the Email/Text analysis tab, showcasing the clean, dark-mode design, input area, and real-time status display.*

!https://www.wowhead.com/quest=83328/the-analysis-interface(image_3.png)
*Figure 3: The interface for pasting and analyzing URLs, providing clear instructions for the user to insert a link for scanning.*

![File Analysis Interface](image_4.png)
*Figure 4: The file analysis tab, featuring "Drag & Drop" support for files, a "Browse File" button, and a checkbox for activating the real-time "Downloads" folder radar.*

![Error and Warning List](image_1.png)
*Figure 5: A screenshot showing an error-free (0 Errors) list with manageable warnings (CS8625 and CS8618) during development, confirming a stable build.*
## 📖 Overview
CyberSecurity Assistant is a modern, high-performance security tool designed to protect users from malicious files, harmful links, and social engineering attacks. Operating as a standalone executable (Self-contained), it integrates seamlessly into the Windows OS without requiring any external dependencies or framework installations.

## ✨ Key Features

* **📡 Real-Time Downloads Radar:** Actively monitors the system's "Downloads" folder 24/7. It intelligently filters out temporary browser files (`.crdownload`, `.tmp`) and automatically intercepts and scans files the moment a download completes.
* **🧠 Multi-Vector Threat Analysis:**
  * **File & URL Scanner:** Integrates with the **VirusTotal API** to query files and links against 70+ global antivirus engines in real-time.
  * **Phishing Detection:** Utilizes a custom Natural Language Processing (NLP) scoring algorithm to analyze text/emails for urgency, blackmail, and sensitive data requests.
* **👻 Active Defense & Ghost Mode:** The application can run silently in the system tray (background). If a critical threat is detected, it pushes a Windows BalloonTip notification and offers a one-click "Delete Threat" action to permanently eradicate the file from the disk.
* **📄 Enterprise Reporting:** Generates dynamic, color-coded, and professionally branded A4 PDF security reports with a single click using the **QuestPDF** library.
* **🎨 Zero-Latency Custom UI:** Features a borderless, dark-mode WPF interface with custom window-snapping mechanics that bypass native Windows animations for a premium, zero-latency user experience.

## 📸 Screenshots

*(Add a screenshot of your application here)*
## 🛠️ Tech Stack
* **Language:** C#
* **Framework:** WPF (Windows Presentation Foundation) / .NET
* **APIs:** VirusTotal API v3
* **Libraries:** QuestPDF (for document generation), MaterialDesignThemes
* **Architecture:** Self-contained deployment (No .NET runtime required on the target machine)

## 🚀 How to Run
Since this application is compiled as a self-contained executable, no installation is required.
1. Download the latest `CyberSecurityAssistant.exe` from the Releases tab (or build it from source).
2. Double-click the file to launch.
3. Keep it running in the system tray for real-time protection.

---
**Author:** [Parsa Karami](https://github.com/karamiparsa325-commits)  
*Developed as a comprehensive project showcasing software architecture, API integration, and UI/UX design.*
