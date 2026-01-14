using System;
using System.Collections.Generic;
using System.Linq;

namespace ComplaintManagementSystem.Services
{
    public class ChatService
    {
        // Knowledge base organized by topics
        private static readonly Dictionary<string, KnowledgeItem> KnowledgeBase = new()
        {
            // ===== Account & Authentication =====
            ["register"] = new KnowledgeItem(
                new[] { "register", "sign up", "signup", "create account", "new account", "join" },
                "To register as a citizen:\n" +
                "1. Click 'Register' on the login page\n" +
                "2. Fill in your name, email, phone, and password\n" +
                "3. Your password must be strong (uppercase, lowercase, number, special character)\n" +
                "4. After registering, verify your email using the OTP code sent to you\n" +
                "5. You can also register with Google for faster access!"
            ),
            ["login"] = new KnowledgeItem(
                new[] { "login", "log in", "sign in", "signin", "access account", "enter" },
                "To login:\n" +
                "1. Go to the Login page\n" +
                "2. Enter your email and password\n" +
                "3. Click 'Remember Me' to stay logged in\n" +
                "4. You can also login with Google!\n\n" +
                "Note: After 5 failed attempts, your account will be temporarily locked for security."
            ),
            ["password_reset"] = new KnowledgeItem(
                new[] { "forgot password", "reset password", "change password", "lost password", "password", "cant login", "can't login" },
                "To reset your password:\n" +
                "1. Click 'Forgot Password' on the login page\n" +
                "2. Enter your registered email address\n" +
                "3. Check your email for the 6-digit verification code\n" +
                "4. Enter the code on the verification page\n" +
                "5. Create a new strong password\n\n" +
                "You can also change your password from your Profile page when logged in."
            ),
            ["email_verify"] = new KnowledgeItem(
                new[] { "verify email", "email verification", "otp", "verification code", "confirm email" },
                "Email verification is required to:\n" +
                "• Ensure you receive important notifications\n" +
                "• Protect your account from unauthorized access\n" +
                "• Enable password reset functionality\n\n" +
                "Check your inbox (and spam folder) for the 6-digit OTP code. The code expires after a short time, but you can request a new one."
            ),
            ["profile"] = new KnowledgeItem(
                new[] { "profile", "update profile", "edit profile", "my account", "account settings", "personal info", "change name", "change phone", "change email" },
                "To manage your profile:\n" +
                "1. Click your name in the top-right corner\n" +
                "2. Select 'Profile'\n" +
                "3. You can update your name, phone number, and address\n" +
                "4. You can also change your password here\n" +
                "5. Click 'Save Changes' when done"
            ),
            ["google_login"] = new KnowledgeItem(
                new[] { "google", "google login", "sso", "social login", "login with google", "google account" },
                "You can login or register using your Google account:\n" +
                "1. Click 'Continue with Google' on the login page\n" +
                "2. Select your Google account\n" +
                "3. If you're new, we'll create an account for you automatically\n" +
                "4. Your experience will be the same as regular registration!"
            ),

            // ===== Complaints =====
            ["submit_complaint"] = new KnowledgeItem(
                new[] { "submit complaint", "create complaint", "new complaint", "file complaint", "make complaint", "report issue", "report problem", "lodge complaint" },
                "To submit a complaint:\n" +
                "1. Navigate to 'Complaints' in the menu\n" +
                "2. Click 'Create New Complaint'\n" +
                "3. Fill in the complaint form:\n" +
                "   • Title: Brief summary of your issue\n" +
                "   • Category: Select the appropriate category\n" +
                "   • Description: Detailed explanation\n" +
                "   • Location: Where the issue occurred (optional map selection)\n" +
                "   • Priority: Low, Medium, High, or Critical\n" +
                "   • Attachments: Upload photos or documents (optional)\n" +
                "4. Choose if you want to submit anonymously\n" +
                "5. Click 'Submit'"
            ),
            ["check_status"] = new KnowledgeItem(
                new[] { "check status", "complaint status", "my complaints", "track complaint", "view complaints", "status update", "where is my", "track" },
                "To check your complaint status:\n" +
                "1. Go to 'My Complaints' in the navigation\n" +
                "2. You'll see a list of all your submitted complaints\n" +
                "3. Each complaint shows its current status:\n" +
                "   • Pending: Awaiting review\n" +
                "   • In Progress: Being handled by staff\n" +
                "   • Resolved: Completed\n" +
                "   • Rejected: Not accepted (with reason)\n" +
                "4. Click on any complaint to see full details and history"
            ),
            ["anonymous"] = new KnowledgeItem(
                new[] { "anonymous", "anonymously", "hide identity", "without name", "privacy", "private complaint" },
                "You can submit complaints anonymously:\n" +
                "• Check the 'Submit Anonymously' option when creating a complaint\n" +
                "• Your personal details (name, email, phone) will be hidden from staff\n" +
                "• You'll still receive status updates and notifications\n" +
                "• Only senior administrators can view your identity if absolutely necessary"
            ),
            ["attachments"] = new KnowledgeItem(
                new[] { "attachment", "upload", "photo", "image", "document", "file", "evidence", "picture" },
                "You can add attachments to your complaints:\n" +
                "• Supported formats: Images (JPG, PNG), Documents (PDF)\n" +
                "• You can upload multiple files\n" +
                "• Attachments help staff understand and resolve your issue faster\n" +
                "• Photos of the problem location are especially helpful!"
            ),
            ["priority"] = new KnowledgeItem(
                new[] { "priority", "urgent", "emergency", "critical", "important", "how urgent" },
                "Complaint priorities help us respond appropriately:\n" +
                "• Low: Minor issues that can wait\n" +
                "• Medium: Standard issues (default)\n" +
                "• High: Important issues needing quick attention\n" +
                "• Critical: Emergencies requiring immediate action\n\n" +
                "Higher priority complaints are addressed faster based on our SLA targets."
            ),
            ["sla"] = new KnowledgeItem(
                new[] { "sla", "how long", "response time", "resolution time", "when resolved", "timeframe", "how fast" },
                "Our response times depend on complaint priority and category:\n" +
                "• We aim to acknowledge all complaints within 24 hours\n" +
                "• Standard resolution target is within 72 hours (3 business days)\n" +
                "• Critical complaints get priority handling\n" +
                "• You'll receive notifications at each stage of the process"
            ),
            ["complaint_statuses"] = new KnowledgeItem(
                new[] { "status meaning", "what does pending mean", "what does resolved mean", "status types", "complaint stages" },
                "Complaint status meanings:\n" +
                "• Pending: Your complaint has been received and is waiting for review\n" +
                "• In Progress: A staff member is actively working on your issue\n" +
                "• Resolved: Your complaint has been addressed and closed\n" +
                "• Rejected: The complaint couldn't be processed (reason provided)\n\n" +
                "You can view the complete history of status changes in the complaint details."
            ),
            ["edit_complaint"] = new KnowledgeItem(
                new[] { "edit complaint", "change complaint", "update complaint", "modify complaint" },
                "You can edit your complaint while it's still in 'Pending' status:\n" +
                "1. Go to 'My Complaints'\n" +
                "2. Click on the complaint you want to edit\n" +
                "3. Click the 'Edit' button\n" +
                "4. Make your changes and save\n\n" +
                "Note: Once a complaint is 'In Progress' or beyond, you cannot edit it. Contact support if urgent changes are needed."
            ),
            ["delete_complaint"] = new KnowledgeItem(
                new[] { "delete complaint", "remove complaint", "cancel complaint", "withdraw complaint" },
                "You can delete your complaint if it's still in 'Pending' status:\n" +
                "1. Go to 'My Complaints'\n" +
                "2. Click on the complaint\n" +
                "3. Click the 'Delete' button and confirm\n\n" +
                "Once deleted, this action cannot be undone."
            ),

            // ===== Categories & Departments =====
            ["categories"] = new KnowledgeItem(
                new[] { "category", "categories", "type of complaint", "what types", "complaint types" },
                "Complaints are organized into categories such as:\n" +
                "• Infrastructure issues\n" +
                "• Public services\n" +
                "• Environmental concerns\n" +
                "• Safety and security\n" +
                "• And more...\n\n" +
                "Categories help route your complaint to the right department automatically."
            ),
            ["departments"] = new KnowledgeItem(
                new[] { "department", "departments", "who handles", "which team", "assigned to" },
                "Your complaint is automatically routed to the appropriate department based on:\n" +
                "• The category you select\n" +
                "• The location of the issue\n" +
                "• The priority level\n\n" +
                "Each department has specialized staff to handle specific types of issues."
            ),

            // ===== Notifications =====
            ["notifications"] = new KnowledgeItem(
                new[] { "notification", "notifications", "updates", "alerts", "get notified", "how do i know" },
                "Stay updated on your complaints:\n" +
                "• In-app notifications appear in the bell icon (top-right)\n" +
                "• Email notifications for important updates\n" +
                "• Real-time status change alerts\n\n" +
                "Go to the Notifications page to see all your notifications and mark them as read."
            ),

            // ===== Feedback =====
            ["feedback"] = new KnowledgeItem(
                new[] { "feedback", "rating", "rate", "review", "satisfaction", "happy", "unhappy" },
                "After your complaint is resolved:\n" +
                "• You can rate the resolution (1-5 stars)\n" +
                "• Provide comments about your experience\n" +
                "• Your feedback helps us improve our services\n\n" +
                "Go to the resolved complaint and click 'Give Feedback'."
            ),

            // ===== Staff Features =====
            ["staff_role"] = new KnowledgeItem(
                new[] { "what can staff do", "staff role", "staff features", "department staff" },
                "Staff members can:\n" +
                "• View and manage complaints assigned to their department\n" +
                "• Update complaint status (Pending → In Progress → Resolved)\n" +
                "• Add notes and updates to complaints\n" +
                "• Communicate with citizens via private chat\n" +
                "• View complaint history and attachments"
            ),
            ["staff_update"] = new KnowledgeItem(
                new[] { "update status", "change status", "mark resolved", "in progress", "close complaint" },
                "Staff can update complaint status:\n" +
                "1. Go to the complaint details\n" +
                "2. Click 'Update Status'\n" +
                "3. Select the new status\n" +
                "4. Add notes explaining the update\n" +
                "5. The citizen will be notified automatically"
            ),

            // ===== Admin Features =====
            ["admin_role"] = new KnowledgeItem(
                new[] { "what can admin do", "admin role", "admin features", "administrator" },
                "Administrators can:\n" +
                "• View dashboard with statistics and analytics\n" +
                "• Manage all complaints across departments\n" +
                "• Create and manage categories\n" +
                "• Manage departments and staff accounts\n" +
                "• View and manage citizen accounts\n" +
                "• Link related or duplicate complaints\n" +
                "• Generate reports"
            ),
            ["manage_staff"] = new KnowledgeItem(
                new[] { "manage staff", "add staff", "create staff", "staff management" },
                "Admins can manage staff accounts:\n" +
                "1. Go to Admin → Staff\n" +
                "2. Create new staff accounts with department assignment\n" +
                "3. Activate or deactivate staff members\n" +
                "4. Update staff information"
            ),
            ["manage_departments"] = new KnowledgeItem(
                new[] { "manage department", "add department", "create department", "department management" },
                "Admins can manage departments:\n" +
                "1. Go to Admin → Departments\n" +
                "2. Create new departments with name, location, contact info\n" +
                "3. Update department details\n" +
                "4. Assign categories to departments for auto-routing"
            ),
            ["manage_categories"] = new KnowledgeItem(
                new[] { "manage category", "add category", "create category", "category management" },
                "Admins can manage categories:\n" +
                "1. Go to Admin → Categories\n" +
                "2. Create new complaint categories\n" +
                "3. Set risk levels and SLA targets\n" +
                "4. Assign default departments for auto-routing"
            ),
            ["manage_citizens"] = new KnowledgeItem(
                new[] { "manage citizen", "citizen management", "lock account", "unlock account", "user management" },
                "Admins can manage citizen accounts:\n" +
                "1. Go to Admin → Citizens\n" +
                "2. View all registered citizens\n" +
                "3. Lock/unlock accounts if needed\n" +
                "4. Flag vulnerable citizens for priority handling"
            ),

            // ===== Private Chat =====
            ["private_chat"] = new KnowledgeItem(
                new[] { "private chat", "chat with staff", "message staff", "direct message", "talk to staff" },
                "Private Chat allows direct communication:\n" +
                "• Citizens can chat with staff handling their complaint\n" +
                "• Real-time messaging for quick clarifications\n" +
                "• Share additional information about your complaint\n" +
                "• Access from the Private Chat section in the menu"
            ),

            // ===== Privacy & Security =====
            ["data_security"] = new KnowledgeItem(
                new[] { "secure", "security", "data", "safe", "protect", "encryption" },
                "We take your data security seriously:\n" +
                "• All personal data is encrypted\n" +
                "• Passwords are hashed and never stored in plain text\n" +
                "• Only authorized personnel can access your information\n" +
                "• Anonymous complaints protect your identity\n" +
                "• Regular security audits are performed"
            ),
            ["who_sees"] = new KnowledgeItem(
                new[] { "who sees", "who can see", "visible", "access my complaint", "confidential" },
                "Who can see your complaint:\n" +
                "• You (the citizen who submitted it)\n" +
                "• Staff in the assigned department\n" +
                "• System administrators\n\n" +
                "If you submit anonymously, staff will only see the complaint content, not your personal details."
            ),
            ["privacy_policy"] = new KnowledgeItem(
                new[] { "privacy policy", "data policy", "how data used", "terms" },
                "Our Privacy Policy explains:\n" +
                "• What data we collect and why\n" +
                "• How we protect your information\n" +
                "• Your rights regarding your data\n\n" +
                "Read the full policy at Help → Privacy Policy."
            ),

            // ===== Help & Contact =====
            ["help"] = new KnowledgeItem(
                new[] { "help", "support", "assistance", "guide", "how to" },
                "Need help? Here are your options:\n" +
                "• Visit the Help Center for FAQs\n" +
                "• Search for answers using the FAQ search\n" +
                "• Contact our support team\n" +
                "• Chat with me anytime for quick assistance!\n\n" +
                "Navigate to 'Help' in the menu for more resources."
            ),
            ["contact"] = new KnowledgeItem(
                new[] { "contact", "email support", "support team", "get in touch", "reach out" },
                "To contact our support team:\n" +
                "1. Navigate to 'Contact' in the menu\n" +
                "2. Fill out the contact form\n" +
                "3. We'll respond to your inquiry as soon as possible\n\n" +
                "For complaint-specific issues, you can also use Private Chat to message staff directly."
            ),
            ["faq"] = new KnowledgeItem(
                new[] { "faq", "frequently asked", "common questions", "questions" },
                "Visit our FAQ section for answers to common questions:\n" +
                "• Account & Login issues\n" +
                "• How to submit and track complaints\n" +
                "• Privacy and data policies\n\n" +
                "Navigate to Help → FAQ or use the search bar to find specific answers."
            ),

            // ===== Reports =====
            ["reports"] = new KnowledgeItem(
                new[] { "report", "reports", "statistics", "analytics", "chart", "trends" },
                "Reports provide insights into complaint management:\n" +
                "• Staff can view department performance\n" +
                "• Admins can see system-wide statistics\n" +
                "• Charts show complaint trends over time\n" +
                "• Resolution rates and SLA compliance metrics\n\n" +
                "Access Reports from the navigation menu (Staff/Admin only)."
            ),

            // ===== Announcements =====
            ["announcements"] = new KnowledgeItem(
                new[] { "announcement", "announcements", "news", "updates", "what's new" },
                "Stay informed with system announcements:\n" +
                "• View the latest news and updates\n" +
                "• Important service notifications\n" +
                "• Planned maintenance schedules\n\n" +
                "Check the Announcements page regularly for updates."
            )
        };

        // Greeting patterns
        private static readonly string[] Greetings = { "hello", "hi", "hey", "good morning", "good afternoon", "good evening", "howdy", "greetings" };
        private static readonly string[] Thanks = { "thank you", "thanks", "thx", "appreciated", "grateful" };
        private static readonly string[] Goodbyes = { "bye", "goodbye", "see you", "later", "take care", "gotta go" };

        public string GetResponse(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return "I didn't catch that. Could you please type your question?";
            }

            message = message.ToLower().Trim();

            // Check for greetings
            if (Greetings.Any(g => message.Contains(g)))
            {
                var greetingResponses = new[]
                {
                    "Hello! 👋 Welcome to the Complaint Management System. How can I help you today?",
                    "Hi there! 😊 I'm your virtual assistant. What would you like to know about our system?",
                    "Hey! Welcome! I can help you with complaints, account issues, or any questions about our system.",
                    "Greetings! I'm here to assist you with the Complaint Management System. What can I do for you?"
                };
                return greetingResponses[new Random().Next(greetingResponses.Length)];
            }

            // Check for thanks
            if (Thanks.Any(t => message.Contains(t)))
            {
                var thankResponses = new[]
                {
                    "You're welcome! 😊 Is there anything else I can help you with?",
                    "Happy to help! Feel free to ask if you have more questions.",
                    "My pleasure! Let me know if you need anything else.",
                    "Glad I could assist! I'm here if you need more help."
                };
                return thankResponses[new Random().Next(thankResponses.Length)];
            }

            // Check for goodbyes
            if (Goodbyes.Any(g => message.Contains(g)))
            {
                var byeResponses = new[]
                {
                    "Goodbye! 👋 Have a great day!",
                    "Take care! Come back anytime you need help.",
                    "See you later! Good luck with everything!",
                    "Bye for now! Don't hesitate to reach out if you need assistance."
                };
                return byeResponses[new Random().Next(byeResponses.Length)];
            }

            // Search knowledge base for matching topic
            foreach (var item in KnowledgeBase.Values)
            {
                if (item.Keywords.Any(keyword => message.Contains(keyword)))
                {
                    return item.Response;
                }
            }

            // Fallback with helpful suggestions
            return "I'm not sure I understand that question. 🤔\n\n" +
                   "Here are some things I can help you with:\n" +
                   "• 📝 How to submit or track complaints\n" +
                   "• 🔐 Account, login, and password issues\n" +
                   "• 🔔 Notifications and updates\n" +
                   "• 👥 Staff and admin features\n" +
                   "• 🔒 Privacy and security\n" +
                   "• ❓ FAQs and help resources\n\n" +
                   "Try asking something like:\n" +
                   "\"How do I submit a complaint?\"\n" +
                   "\"How do I reset my password?\"\n" +
                   "\"Who can see my complaint?\"";
        }

        private class KnowledgeItem
        {
            public string[] Keywords { get; }
            public string Response { get; }

            public KnowledgeItem(string[] keywords, string response)
            {
                Keywords = keywords;
                Response = response;
            }
        }
    }
}
