namespace NovaPass_API.Models;

public enum UserRole
{
    customer,
    seller,
    scanner,
    admin
}

public enum EventStatus
{
    active,
    cancelled,
    sold_out
}

public enum TicketStatus
{
    pending,
    active,
    used,
    cancelled
}

public enum PaymentStatus
{
    pending,
    approved,
    rejected
}

public enum PqrsType
{
    question,
    complaint,
    claim,
    suggestion
}

public enum PqrsStatus
{
    pending,
    in_progress,
    resolved,
    closed
}
