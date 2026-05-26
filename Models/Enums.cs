namespace NovaPass_API.Models;

public enum UserRol { customer, admin, seller, scanner }
public enum EventStatus { active, cancelled, finished }
public enum TicketStatus { available, sold, used, cancelled }
public enum PqrsType { petition, complaint, claim, suggestion }
public enum PqrsStatus { pending, in_progress, resolved }
public enum PaymentStatus { pending, approved, rejected, refunded }
