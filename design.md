# TutorMatch --- Frontend Design System & Product Context

## 1. Product overview

**TutorMatch** is a peer-to-peer tutoring platform for university
students. A student describes what they need help with in natural
language (for example, "I need help understanding pointers"), and the
platform helps them find a suitable tutor. Alongside AI-powered semantic
search, users can search with traditional filters.

The product should feel like a polished, trustworthy, modern education
SaaS product---not a generic school portal and not a marketing-only
landing page.

### Core product areas

-   Discover tutors through natural-language semantic search.
-   Search and filter tutors by subject, hourly price, and availability.
-   View tutor profiles, subjects, hourly rates, ratings, biographies,
    and available time slots.
-   Book a tutoring session.
-   View upcoming, completed, and cancelled sessions.
-   Cancel a session and clearly understand whether the cancellation is
    late.
-   Mark an eligible session as completed.
-   Rate a tutor after a completed session, once per session.
-   Register as a tutor, edit tutor details, and manage availability.

## 2. Design direction

### Overall feel

-   Premium, minimal, modern, calm, and professional.
-   Inspiration: the clarity and speed of Uber-like consumer SaaS
    interfaces, adapted for a university tutoring product.
-   Strong information hierarchy, generous whitespace, tidy alignment,
    readable typography, and consistent spacing.
-   Friendly enough for students, but not childish.
-   Avoid generic "AI neon" visuals, excessive gradients, glassmorphism,
    noisy backgrounds, and oversized decorative illustrations.
-   Use subtle transitions and restrained micro-interactions; the UI
    must remain fast and accessible.

### Suggested visual identity

-   Primary: deep indigo / midnight navy.
-   Accent: fresh violet or blue-violet for primary actions and
    AI-related highlights.
-   Background: very light cool gray, with white surfaces.
-   Text: dark slate / near-black for primary text; muted slate for
    secondary text.
-   Success: restrained green.
-   Warning / late cancellation: amber.
-   Error: red used only for actual errors and destructive actions.
-   Use color consistently; do not rely on color alone to communicate
    status.

### Typography

-   Use a modern sans-serif such as Inter.
-   Clear scale for page titles, section headings, card titles, body
    text, labels, and helper text.
-   Avoid tiny labels and low-contrast secondary text.
-   Use sentence case for headings and buttons.

### Components and surfaces

-   Medium rounded corners, not pill-shaped everything.
-   Light borders and subtle shadows; avoid heavy shadows.
-   Consistent button heights, input heights, card padding, and spacing.
-   Use Lucide-style line icons consistently.
-   Tutor cards should be information-rich but scannable.
-   Use skeletons for loading states, empty states with helpful next
    steps, and clear inline validation.

## 3. Information architecture

Create a coherent application shell with: - A top navigation bar or
compact sidebar, whichever best supports the screen layout. - TutorMatch
logo / wordmark. - Main navigation: Discover tutors, My sessions, Become
a tutor / Tutor profile. - User avatar menu with profile and sign-out
options if an authenticated state is shown. - Responsive mobile
navigation. - Clear active navigation state.

The most important screen is **Discover tutors**. Prioritize the search
experience and tutor result cards over decorative marketing content.

## 4. Required screens

### A. Discover tutors / Search

This is the primary student screen.

Include: - Page heading and a concise supporting sentence. - A prominent
natural-language search field with placeholder: "Describe what you need
help with..." - Example prompts/chips such as "Pointers in C", "Sorting
algorithms", "Binary trees", and "Object-oriented programming". - A
clear Search action. - Traditional filters: subject, hourly price range,
and availability/date. - Sort control and result count. - Tutor result
cards with avatar, name, short bio, subjects/tags, hourly price, average
rating, and next available slot when provided. - Clear action to open
the tutor profile and a secondary booking action if appropriate. -
Loading, no-results, and error states. - A small, unobtrusive
search-mode indicator for `semantic` or `keyword`. If semantic search
falls back to keyword search, show a helpful message without alarming
the user.

Do not invent claims about AI accuracy or promise that a match is
guaranteed.

### B. Tutor profile

Include: - Tutor avatar, name, rating summary, hourly price, and concise
biography. - Subject tags and relevant tutoring topics. - Availability
selector with dates and time slots. - Clear booking call to action. -
Helpful unavailable / no-slots state. - Accessible presentation of
rating and review content. - Avoid showing a booking as successful until
the UI receives confirmation.

### C. Booking flow

Include: - Selected tutor and selected time slot summary. - Date/time
confirmation. - Clear confirm-booking action. - Loading, success, and
error states. - For a slot conflict (`409`), explain that the slot was
just taken and let the student choose another time. - If the student has
reached the daily limit of two sessions, explain the limit clearly and
do not present the booking as successful.

### D. My sessions

Provide clear categories or tabs for: - Upcoming - Completed - Cancelled

Each session item should show tutor/student context as appropriate,
subject, date, time, status, and relevant actions. - Upcoming sessions
may offer Cancel. - Completed sessions may offer Rate tutor if the
session has not already been rated. - Eligible sessions may offer Mark
as completed to the tutor. - Cancelled and late-cancelled sessions
should have distinct, understandable status labels. - Include empty
states for each category.

### E. Tutor registration / Edit tutor profile

Include a clean form with: - Short biography. - Subjects / topics
taught. - Hourly rate. - Availability editor that allows users to add
and remove date/time intervals. - Inline validation and helpful error
messages. - Save action, loading state, success feedback, and failure
feedback. - Prevent confusing overlapping availability intervals in the
UI where possible, while treating the backend as authoritative.

### F. Rating form

Include: - Five-star rating input. - Optional written comment. - Submit
action and validation. - A clear indication that ratings are available
only after a session is completed and only once per session. - Success
and error states.

## 5. Domain rules that the UI must respect

These are product rules, not visual suggestions:

1.  A tutor cannot have overlapping booked sessions.
2.  If two users attempt to book the same slot concurrently, one may
    receive HTTP `409`; the interface must handle this gracefully.
3.  Cancellation is free when made at least 12 hours before the session
    starts. Later cancellation becomes `LateCancelled`; clearly warn the
    user before confirmation when this applies.
4.  A student can have at most two sessions per day.
5.  A rating can be submitted only for a `Completed` session and only
    once per session.
6.  Tutor availability is represented as date/time intervals. The UI
    should make interval boundaries easy to understand.
7.  Semantic search returns up to five best-matching tutors. If the AI
    service is unavailable, the system can fall back to classic keyword
    search. Reflect the returned search mode in the UI rather than
    pretending semantic search succeeded.

## 6. Interaction and state requirements

Design the important states, not just the happy path: - Default,
focused, filled, disabled, and invalid form fields. - Loading / skeleton
state. - Empty search results. - API error state with retry where
appropriate. - Booking success and slot-conflict (`409`) states. - Free
cancellation versus late cancellation warning. - No available slots. -
Session status transitions. - Already-rated session. - Semantic search
fallback to keyword mode. - Mobile layout and responsive behavior.

Use accessible contrast, visible keyboard focus, semantic labels, clear
error messages, and sufficiently large interactive targets.

## 7. Responsive behavior

-   Desktop: use the available space efficiently, with a readable
    content width and well-aligned result cards / filters.
-   Tablet: allow filters to collapse into a drawer or compact panel.
-   Mobile: single-column cards, full-width search input, touch-friendly
    controls, and a compact navigation pattern.
-   Keep the most important action visible without making the screen
    feel crowded.

## 8. Content and sample data

Use realistic but fictional university tutoring data. Subjects can
include: - Algorithms and data structures - C / C++ - Java -
Object-oriented programming - Databases and SQL - Discrete mathematics

Use Romanian UI copy if designing specifically for Romanian university
students. Keep terminology consistent and natural. Example search
placeholder in Romanian: **„Descrie problema la care ai nevoie de
ajutor..."**. Example subject tags: „Algoritmi", „Structuri de date",
„C++", „SQL".

Do not use real personal information. Do not fill the interface with
lorem ipsum when meaningful product copy can be used.

## 9. Implementation handoff

The design will later be implemented in React. Make the UI consistent
enough to translate into reusable components: - App shell / navigation -
Search bar and example prompts - Filter controls - Tutor card - Rating
display and rating form - Availability / time-slot picker - Session card
and status badge - Empty state, skeleton, error message, confirmation
dialog - Form fields and buttons

Use a consistent spacing scale, typography scale, color tokens, radius,
border, and shadow rules. Keep layouts and component patterns consistent
across screens.

The existing Stitch design is the visual source of truth during
implementation: preserve its layout, spacing, typography, hierarchy,
colors, and overall style rather than redesigning it from scratch.

## 10. Out of scope / do not assume

-   Do not invent payment processing, chat, video calls, calendar
    integrations, notifications, or OAuth flows unless the team
    explicitly adds them.
-   Do not imply that a session is booked, cancelled, completed, or
    rated before the backend confirms the operation.
-   Do not expose backend implementation details to end users.
-   Do not make the entire product a landing page; prioritize the
    signed-in application experience.
-   Authentication / user identity details are still to be decided by
    the team, so use a neutral demo user state and avoid designing an
    elaborate sign-up/authentication system as a dependency.

## 11. Success criteria

The resulting design should make it immediately obvious: 1. How a
student describes a problem and finds a tutor. 2. How a student compares
tutors and selects an available slot. 3. Where a user manages their
sessions. 4. How a tutor manages profile information and availability.
5. Which actions are allowed based on session status. 6. What happens
when a slot is taken, cancellation is late, or AI search falls back to
keyword mode.

The final UI should look cohesive, implementation-ready, responsive, and
suitable for a university project demo.
