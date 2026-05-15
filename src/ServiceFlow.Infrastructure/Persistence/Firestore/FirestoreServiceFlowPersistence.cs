using Google.Cloud.Firestore;
using ServiceFlow.Application.Common.Abstractions;
using ServiceFlow.Application.ServiceOrders;
using ServiceFlow.Domain.Customers;
using ServiceFlow.Domain.RepairRequests;
using ServiceFlow.Domain.ServiceOrders;
using ServiceFlow.Domain.Users;
using ServiceFlow.Domain.Vehicles;

namespace ServiceFlow.Infrastructure.Persistence.Firestore;

internal sealed class FirestoreServiceFlowPersistence(FirestoreDb db) : IServiceFlowPersistence
{
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public async Task<bool> UserExistsWithEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        var q = db.Collection(FirestoreCollections.Users).WhereEqualTo("email", normalizedEmail).Limit(1);
        var snap = await q.GetSnapshotAsync(cancellationToken);
        return snap.Count > 0;
    }

    public async Task AddUserAsync(User user, CancellationToken cancellationToken = default)
    {
        await WriteUserAsync(user, cancellationToken);
    }

    public async Task<User?> FindUserByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        var q = db.Collection(FirestoreCollections.Users).WhereEqualTo("email", normalizedEmail).Limit(1);
        var snap = await q.GetSnapshotAsync(cancellationToken);
        if (snap.Count == 0) return null;
        return FirestoreUserMapper.ToUser(snap.Documents[0]);
    }

    public async Task<User?> FindUserByIdReadOnlyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var snap = await db.Collection(FirestoreCollections.Users).Document(id.ToString()).GetSnapshotAsync(cancellationToken);
        return snap.Exists ? FirestoreUserMapper.ToUser(snap) : null;
    }

    public async Task<bool> AnyActiveStaffOrAdminAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await FindUserByIdReadOnlyAsync(userId, cancellationToken);
        return user is { IsActive: true, Role: UserRole.Staff or UserRole.Admin };
    }

    public async Task AddCustomerAsync(Customer customer, CancellationToken cancellationToken = default) =>
        await WriteCustomerAsync(customer, cancellationToken);

    public Task<Customer?> FindCustomerByIdTrackedAsync(Guid id, CancellationToken cancellationToken = default) =>
        FindCustomerByIdReadOnlyAsync(id, cancellationToken);

    public async Task<Customer?> FindCustomerByIdReadOnlyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var snap = await db.Collection(FirestoreCollections.Customers).Document(id.ToString()).GetSnapshotAsync(cancellationToken);
        return snap.Exists ? MapCustomer(snap) : null;
    }

    public async Task<(IReadOnlyList<Customer> Items, int Total)> SearchCustomersAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var all = await db.Collection(FirestoreCollections.Customers).GetSnapshotAsync(cancellationToken);
        var list = all.Documents.Select(MapCustomer).Where(c => c is not null).Select(c => c!).ToList();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            list = list.Where(c =>
                c.FullName.ToLower().Contains(s) ||
                (c.Email != null && c.Email.ToLower().Contains(s)) ||
                c.PhoneNumber.Contains(s)).ToList();
        }

        var total = list.Count;
        var items = list
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        return (items, total);
    }

    public async Task<Guid?> FindCustomerIdByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var uid = userId.ToString();
        var all = await db.Collection(FirestoreCollections.Customers).GetSnapshotAsync(cancellationToken);
        foreach (var doc in all.Documents)
        {
            if (doc.TryGetValue("userId", out string u) && u == uid && Guid.TryParse(doc.Id, out var cid))
            {
                return cid;
            }
        }

        return null;
    }

    public async Task<Guid?> FindUserIdByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var c = await FindCustomerByIdReadOnlyAsync(customerId, cancellationToken);
        return c?.UserId;
    }

    public async Task<bool> CustomerExistsAsync(Guid customerId, CancellationToken cancellationToken = default) =>
        (await FindCustomerByIdReadOnlyAsync(customerId, cancellationToken)) is not null;

    public async Task<bool> VehicleLicensePlateExistsAsync(string normalizedPlate, CancellationToken cancellationToken = default)
    {
        var q = db.Collection(FirestoreCollections.Vehicles).WhereEqualTo("licensePlate", normalizedPlate).Limit(1);
        var snap = await q.GetSnapshotAsync(cancellationToken);
        return snap.Count > 0;
    }

    public async Task AddVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken = default) =>
        await WriteVehicleAsync(vehicle, cancellationToken);

    public Task<Vehicle?> FindVehicleByIdTrackedAsync(Guid id, CancellationToken cancellationToken = default) =>
        FindVehicleByIdReadOnlyAsync(id, cancellationToken);

    public async Task<Vehicle?> FindVehicleByIdReadOnlyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var snap = await db.Collection(FirestoreCollections.Vehicles).Document(id.ToString()).GetSnapshotAsync(cancellationToken);
        return snap.Exists ? MapVehicle(snap) : null;
    }

    public Task<Vehicle?> FindVehicleByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        FindVehicleByIdReadOnlyAsync(id, cancellationToken);

    public async Task<IReadOnlyList<Vehicle>> ListVehiclesByCustomerReadOnlyAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var q = db.Collection(FirestoreCollections.Vehicles).WhereEqualTo("customerId", customerId.ToString());
        var snap = await q.GetSnapshotAsync(cancellationToken);
        return snap.Documents.Select(MapVehicle).Where(v => v is not null).Select(v => v!).OrderByDescending(v => v.CreatedAt).ToList();
    }

    public async Task AddServiceOrderAsync(ServiceOrder order, CancellationToken cancellationToken = default) =>
        await WriteServiceOrderAsync(order, cancellationToken);

    public Task<ServiceOrder?> FindServiceOrderForTransitionAsync(Guid id, CancellationToken cancellationToken = default) =>
        LoadFullOrderAsync(id, includeRepairMedia: false, cancellationToken);

    public Task<ServiceOrder?> FindServiceOrderForAssignAsync(Guid id, CancellationToken cancellationToken = default) =>
        LoadFullOrderAsync(id, includeRepairMedia: false, cancellationToken);

    public async Task<(ServiceOrder Order, Customer Customer, Vehicle Vehicle, User? Staff)?> GetServiceOrderDetailBundleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var order = await LoadFullOrderAsync(id, includeRepairMedia: true, cancellationToken);
        if (order is null) return null;

        var customer = await FindCustomerByIdReadOnlyAsync(order.CustomerId, cancellationToken);
        var vehicle = await FindVehicleByIdReadOnlyAsync(order.VehicleId, cancellationToken);
        if (customer is null || vehicle is null) return null;

        User? staff = null;
        if (order.AssignedStaffId is { } sid)
        {
            staff = await FindUserByIdReadOnlyAsync(sid, cancellationToken);
        }

        return (order, customer, vehicle, staff);
    }

    public async Task<(IReadOnlyList<(ServiceOrder Order, Customer Customer, Vehicle Vehicle)> Rows, int Total)>
        ListServiceOrdersAsync(
            ServiceOrderListFilter filter,
            Guid? restrictToCustomerId,
            CancellationToken cancellationToken = default)
    {
        var ordersSnap = await db.Collection(FirestoreCollections.ServiceOrders).GetSnapshotAsync(cancellationToken);
        var orders = new List<ServiceOrder>();
        foreach (var doc in ordersSnap.Documents)
        {
            var o = await MapServiceOrderDocumentAsync(doc, includeRepairMedia: false, loadRepairs: false, cancellationToken);
            if (o is null) continue;

            if (restrictToCustomerId is { } rc && o.CustomerId != rc) continue;
            if (restrictToCustomerId is null)
            {
                if (filter.CustomerId is { } fc && o.CustomerId != fc) continue;
                if (filter.AssignedStaffId is { } fa && o.AssignedStaffId != fa) continue;
            }

            if (filter.Stage is { } st && o.Stage != st) continue;
            orders.Add(o);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim().ToLower();
            var filtered = new List<ServiceOrder>();
            foreach (var o in orders)
            {
                var c = await FindCustomerByIdReadOnlyAsync(o.CustomerId, cancellationToken);
                var v = await FindVehicleByIdReadOnlyAsync(o.VehicleId, cancellationToken);
                if (c is null || v is null) continue;
                if (c.FullName.ToLower().Contains(s) ||
                    v.LicensePlate.ToLower().Contains(s) ||
                    v.Make.ToLower().Contains(s) ||
                    v.Model.ToLower().Contains(s))
                {
                    filtered.Add(o);
                }
            }

            orders = filtered;
        }

        var total = orders.Count;
        var page = orders
            .OrderByDescending(o => o.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToList();

        var rows = new List<(ServiceOrder, Customer, Vehicle)>();
        foreach (var o in page)
        {
            var c = await FindCustomerByIdReadOnlyAsync(o.CustomerId, cancellationToken);
            var v = await FindVehicleByIdReadOnlyAsync(o.VehicleId, cancellationToken);
            if (c is not null && v is not null)
            {
                rows.Add((o, c, v));
            }
        }

        return (rows, total);
    }

    public async Task<RepairRequest?> FindRepairRequestTrackedWithMediaAsync(Guid id, CancellationToken cancellationToken = default) =>
        await LoadRepairAsync(id, cancellationToken);

    public Task<RepairRequest?> FindRepairRequestReadOnlyWithMediaAsync(Guid id, CancellationToken cancellationToken = default) =>
        LoadRepairAsync(id, cancellationToken);

    public async Task<IReadOnlyList<RepairRequest>> ListRepairRequestsByServiceOrderReadOnlyAsync(
        Guid serviceOrderId,
        CancellationToken cancellationToken = default)
    {
        var q = db.Collection(FirestoreCollections.RepairRequests).WhereEqualTo("serviceOrderId", serviceOrderId.ToString());
        var snap = await q.GetSnapshotAsync(cancellationToken);
        var list = new List<RepairRequest>();
        foreach (var doc in snap.Documents)
        {
            var r = MapRepair(doc);
            if (r is not null) list.Add(r);
        }

        return list.OrderByDescending(r => r.CreatedAt).ToList();
    }

    public Task<ServiceOrder?> FindServiceOrderWithHistoryAndRepairsReadOnlyAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        LoadFullOrderAsync(id, includeRepairMedia: false, cancellationToken);

    public Task PersistServiceOrderAsync(ServiceOrder order, CancellationToken cancellationToken = default) =>
        WriteServiceOrderAsync(order, cancellationToken);

    public Task PersistRepairRequestAsync(RepairRequest repair, CancellationToken cancellationToken = default) =>
        WriteRepairRequestAsync(repair, cancellationToken);

    public Task PersistCustomerAsync(Customer customer, CancellationToken cancellationToken = default) =>
        WriteCustomerAsync(customer, cancellationToken);

    public Task PersistVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken = default) =>
        WriteVehicleAsync(vehicle, cancellationToken);

    public async Task PersistNewRepairOnOrderAsync(ServiceOrder order, RepairRequest repair, CancellationToken cancellationToken = default)
    {
        await WriteRepairRequestAsync(repair, cancellationToken);
        await WriteServiceOrderAsync(order, cancellationToken);
    }

    public async Task<Guid?> FindPortalUserIdForServiceOrderAsync(Guid serviceOrderId, CancellationToken cancellationToken = default)
    {
        var order = await LoadFullOrderAsync(serviceOrderId, includeRepairMedia: false, cancellationToken);
        if (order is null) return null;
        return await FindUserIdByCustomerIdAsync(order.CustomerId, cancellationToken);
    }

    private async Task<ServiceOrder?> LoadFullOrderAsync(Guid id, bool includeRepairMedia, CancellationToken cancellationToken)
    {
        var snap = await db.Collection(FirestoreCollections.ServiceOrders).Document(id.ToString()).GetSnapshotAsync(cancellationToken);
        if (!snap.Exists) return null;
        return await MapServiceOrderDocumentAsync(snap, includeRepairMedia, loadRepairs: true, cancellationToken);
    }

    private async Task<ServiceOrder?> MapServiceOrderDocumentAsync(
        DocumentSnapshot snap,
        bool includeRepairMedia,
        bool loadRepairs,
        CancellationToken cancellationToken)
    {
        if (!snap.Exists || !Guid.TryParse(snap.Id, out var id)) return null;

        var customerId = Guid.Parse(snap.GetValue<string>("customerId"));
        var vehicleId = Guid.Parse(snap.GetValue<string>("vehicleId"));
        var assignedRaw = snap.GetValue<string>("assignedStaffId");
        Guid? assignedStaffId = string.IsNullOrEmpty(assignedRaw) ? null : Guid.Parse(assignedRaw);
        var stage = Enum.Parse<ServiceStage>(snap.GetValue<string>("stage"), ignoreCase: true);
        var openingDescription = snap.GetValue<string>("openingDescription");
        var createdAt = snap.GetValue<Timestamp>("createdAt").ToDateTimeOffset();
        var updatedAt = snap.GetValue<Timestamp>("updatedAt").ToDateTimeOffset();
        DateTimeOffset? completedAt = snap.ContainsField("completedAt")
            ? snap.GetValue<Timestamp>("completedAt").ToDateTimeOffset()
            : null;

        var history = new List<ServiceStatusHistoryEntry>();
        if (snap.ContainsField("statusHistory"))
        {
            try
            {
                var rawHist = snap.GetValue<List<Dictionary<string, object>>>("statusHistory");
                foreach (var m in rawHist)
                {
                    var h = MapHistoryEntry(id, m);
                    if (h is not null) history.Add(h);
                }
            }
            catch
            {
                // Emulator / older shapes: ignore history parse errors
            }
        }

        var repairs = new List<RepairRequest>();
        if (loadRepairs && snap.ContainsField("repairRequestIds"))
        {
            List<string> idStrings;
            try
            {
                idStrings = snap.GetValue<List<string>>("repairRequestIds");
            }
            catch
            {
                var objs = snap.GetValue<List<object>>("repairRequestIds");
                idStrings = objs.Select(o => o?.ToString() ?? "").Where(s => s != "").ToList();
            }
            foreach (var ridStr in idStrings)
            {
                if (!Guid.TryParse(ridStr, out _)) continue;
                var rSnap = await db.Collection(FirestoreCollections.RepairRequests).Document(ridStr).GetSnapshotAsync(cancellationToken);
                if (!rSnap.Exists) continue;
                var r = MapRepairCore(rSnap, includeRepairMedia);
                if (r is not null) repairs.Add(r);
            }
        }

        return ServiceOrder.FromPersistence(
            id,
            customerId,
            vehicleId,
            assignedStaffId,
            stage,
            openingDescription,
            completedAt,
            createdAt,
            updatedAt,
            history,
            repairs);
    }

    private static ServiceStatusHistoryEntry? MapHistoryEntry(Guid orderId, Dictionary<string, object> m)
    {
        if (!m.TryGetValue("id", out var idObj) || idObj is not string ids || !Guid.TryParse(ids, out var entryId))
        {
            return null;
        }

        var fromRaw = m.TryGetValue("fromStage", out var fs) ? fs as string : null;
        ServiceStage? fromStage = string.IsNullOrEmpty(fromRaw) ? null : Enum.Parse<ServiceStage>(fromRaw, ignoreCase: true);
        var toStage = Enum.Parse<ServiceStage>(m["toStage"] as string ?? "", ignoreCase: true);
        var changedBy = Guid.Parse(m["changedByUserId"] as string ?? "");
        var changedAt = m["changedAt"] switch
        {
            Timestamp ts => ts.ToDateTimeOffset(),
            DateTime dt => new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc)),
            _ => DateTimeOffset.UtcNow
        };
        var noteRaw = m.TryGetValue("note", out var n) ? n as string : null;
        var note = string.IsNullOrWhiteSpace(noteRaw) ? null : noteRaw;
        return ServiceStatusHistoryEntry.FromPersistence(entryId, orderId, fromStage, toStage, changedBy, changedAt, note);
    }

    private static Customer? MapCustomer(DocumentSnapshot snap)
    {
        if (!snap.Exists || !Guid.TryParse(snap.Id, out var id)) return null;
        var fullName = snap.GetValue<string>("fullName");
        var phoneNumber = snap.GetValue<string>("phoneNumber");
        var emailRaw = snap.ContainsField("email") ? snap.GetValue<string>("email") : null;
        var email = string.IsNullOrWhiteSpace(emailRaw) ? null : emailRaw.Trim().ToLowerInvariant();
        Guid? userId = null;
        if (snap.TryGetValue("userId", out string u) && !string.IsNullOrEmpty(u) && Guid.TryParse(u, out var uid))
        {
            userId = uid;
        }

        var createdAt = snap.ContainsField("createdAt") ? snap.GetValue<Timestamp>("createdAt").ToDateTimeOffset() : DateTimeOffset.UtcNow;
        var updatedAt = snap.ContainsField("updatedAt") ? snap.GetValue<Timestamp>("updatedAt").ToDateTimeOffset() : createdAt;
        return Customer.FromPersistence(id, fullName, phoneNumber, email, userId, createdAt, updatedAt);
    }

    private static Vehicle? MapVehicle(DocumentSnapshot snap)
    {
        if (!snap.Exists || !Guid.TryParse(snap.Id, out var id)) return null;
        var customerId = Guid.Parse(snap.GetValue<string>("customerId"));
        var licensePlate = snap.GetValue<string>("licensePlate");
        var make = snap.GetValue<string>("make");
        var model = snap.GetValue<string>("model");
        var year = (int)snap.GetValue<long>("year");
        var color = snap.ContainsField("color") ? snap.GetValue<string>("color") : null;
        color = string.IsNullOrWhiteSpace(color) ? null : color;
        var vinRaw = snap.ContainsField("vin") ? snap.GetValue<string>("vin") : null;
        var vin = string.IsNullOrWhiteSpace(vinRaw) ? null : vinRaw;
        var createdAt = snap.GetValue<Timestamp>("createdAt").ToDateTimeOffset();
        var updatedAt = snap.GetValue<Timestamp>("updatedAt").ToDateTimeOffset();
        return Vehicle.FromPersistence(id, customerId, licensePlate, make, model, year, color, vin, createdAt, updatedAt);
    }

    private static RepairRequest? MapRepair(DocumentSnapshot snap) => MapRepairCore(snap, includeMedia: true);

    private static RepairRequest? MapRepairCore(DocumentSnapshot snap, bool includeMedia)
    {
        if (!snap.Exists || !Guid.TryParse(snap.Id, out var id)) return null;
        var serviceOrderId = Guid.Parse(snap.GetValue<string>("serviceOrderId"));
        var createdByUserId = Guid.Parse(snap.GetValue<string>("createdByUserId"));
        var issueDescription = snap.GetValue<string>("issueDescription");
        var priceEstimateCents = snap.GetValue<long>("priceEstimateCents");
        var urgency = Enum.Parse<RepairUrgency>(snap.GetValue<string>("urgency"), ignoreCase: true);
        RepairDecision? decision = null;
        if (snap.TryGetValue("customerDecision", out string d) && !string.IsNullOrEmpty(d))
        {
            decision = Enum.Parse<RepairDecision>(d, ignoreCase: true);
        }

        DateTimeOffset? decidedAt = snap.TryGetValue("decidedAt", out Timestamp dts) ? dts.ToDateTimeOffset() : null;
        var decisionNote = snap.TryGetValue("decisionNote", out string dn) ? dn : null;
        var createdAt = snap.GetValue<Timestamp>("createdAt").ToDateTimeOffset();
        var updatedAt = snap.GetValue<Timestamp>("updatedAt").ToDateTimeOffset();

        var media = new List<RepairMedia>();
        if (includeMedia && snap.ContainsField("media"))
        {
            var mediaRaw = snap.GetValue<List<Dictionary<string, object>>>("media");
            foreach (var m2 in mediaRaw)
            {
                var med = MapMedia(id, m2);
                if (med is not null) media.Add(med);
            }
        }

        return RepairRequest.FromPersistence(
            id,
            serviceOrderId,
            createdByUserId,
            issueDescription,
            priceEstimateCents,
            urgency,
            decision,
            decidedAt,
            decisionNote,
            createdAt,
            updatedAt,
            media);
    }

    private async Task<RepairRequest?> LoadRepairAsync(Guid id, CancellationToken cancellationToken)
    {
        var snap = await db.Collection(FirestoreCollections.RepairRequests).Document(id.ToString()).GetSnapshotAsync(cancellationToken);
        return MapRepair(snap);
    }

    private static RepairMedia? MapMedia(Guid repairRequestId, Dictionary<string, object> m)
    {
        if (!m.TryGetValue("id", out var idObj) || idObj is not string ids || !Guid.TryParse(ids, out var mediaId))
        {
            return null;
        }

        var mediaType = Enum.Parse<MediaType>(m["mediaType"] as string ?? "", ignoreCase: true);
        var storagePath = m["storagePath"] as string ?? "";
        var mimeType = m.TryGetValue("mimeType", out var mt) ? mt as string : null;
        var sizeBytes = m.TryGetValue("sizeBytes", out var sb) ? Convert.ToInt64(sb) : 0L;
        var uploadedAt = m["uploadedAt"] switch
        {
            Timestamp ts => ts.ToDateTimeOffset(),
            DateTime dt => new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc)),
            _ => DateTimeOffset.UtcNow
        };
        return RepairMedia.FromPersistence(mediaId, repairRequestId, mediaType, storagePath, mimeType, sizeBytes, uploadedAt);
    }

    private async Task WriteUserAsync(User user, CancellationToken cancellationToken)
    {
        var doc = db.Collection(FirestoreCollections.Users).Document(user.Id.ToString());
        var data = new Dictionary<string, object>
        {
            ["email"] = user.Email,
            ["fullName"] = user.FullName,
            ["passwordHash"] = user.PasswordHash,
            ["role"] = user.Role.ToString(),
            ["isActive"] = user.IsActive,
            ["createdAt"] = Timestamp.FromDateTime(user.CreatedAt.UtcDateTime),
            ["updatedAt"] = Timestamp.FromDateTime(user.UpdatedAt.UtcDateTime)
        };
        await doc.SetAsync(data, cancellationToken: cancellationToken);
    }

    private async Task WriteCustomerAsync(Customer customer, CancellationToken cancellationToken)
    {
        var doc = db.Collection(FirestoreCollections.Customers).Document(customer.Id.ToString());
        var data = new Dictionary<string, object>
        {
            ["fullName"] = customer.FullName,
            ["phoneNumber"] = customer.PhoneNumber,
            ["email"] = customer.Email ?? string.Empty,
            ["userId"] = customer.UserId?.ToString() ?? string.Empty,
            ["createdAt"] = Timestamp.FromDateTime(customer.CreatedAt.UtcDateTime),
            ["updatedAt"] = Timestamp.FromDateTime(customer.UpdatedAt.UtcDateTime)
        };
        await doc.SetAsync(data, cancellationToken: cancellationToken);
    }

    private async Task WriteVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken)
    {
        var doc = db.Collection(FirestoreCollections.Vehicles).Document(vehicle.Id.ToString());
        var data = new Dictionary<string, object>
        {
            ["customerId"] = vehicle.CustomerId.ToString(),
            ["licensePlate"] = vehicle.LicensePlate,
            ["make"] = vehicle.Make,
            ["model"] = vehicle.Model,
            ["year"] = vehicle.Year,
            ["color"] = vehicle.Color ?? string.Empty,
            ["vin"] = vehicle.Vin ?? string.Empty,
            ["createdAt"] = Timestamp.FromDateTime(vehicle.CreatedAt.UtcDateTime),
            ["updatedAt"] = Timestamp.FromDateTime(vehicle.UpdatedAt.UtcDateTime)
        };
        await doc.SetAsync(data, cancellationToken: cancellationToken);
    }

    private async Task WriteServiceOrderAsync(ServiceOrder order, CancellationToken cancellationToken)
    {
        var historyMaps = order.StatusHistory
            .Select(e => new Dictionary<string, object>
            {
                ["id"] = e.Id.ToString(),
                ["serviceOrderId"] = e.ServiceOrderId.ToString(),
                ["fromStage"] = e.FromStage?.ToString() ?? string.Empty,
                ["toStage"] = e.ToStage.ToString(),
                ["changedByUserId"] = e.ChangedByUserId.ToString(),
                ["changedAt"] = Timestamp.FromDateTime(e.ChangedAt.UtcDateTime),
                ["note"] = e.Note ?? string.Empty
            })
            .Cast<object>()
            .ToList();

        var repairIds = order.RepairRequests.Select(r => r.Id.ToString()).ToList();

        var docRef = db.Collection(FirestoreCollections.ServiceOrders).Document(order.Id.ToString());
        var data = new Dictionary<string, object>
        {
            ["customerId"] = order.CustomerId.ToString(),
            ["vehicleId"] = order.VehicleId.ToString(),
            ["assignedStaffId"] = order.AssignedStaffId?.ToString() ?? string.Empty,
            ["stage"] = order.Stage.ToString(),
            ["openingDescription"] = order.OpeningDescription,
            ["createdAt"] = Timestamp.FromDateTime(order.CreatedAt.UtcDateTime),
            ["updatedAt"] = Timestamp.FromDateTime(order.UpdatedAt.UtcDateTime),
            ["statusHistory"] = historyMaps,
            ["repairRequestIds"] = repairIds
        };

        if (order.CompletedAt is { } completed)
        {
            data["completedAt"] = Timestamp.FromDateTime(completed.UtcDateTime);
        }

        await docRef.SetAsync(data, cancellationToken: cancellationToken);
    }

    private async Task WriteRepairRequestAsync(RepairRequest repair, CancellationToken cancellationToken)
    {
        var mediaMaps = repair.Media
            .Select(m => new Dictionary<string, object>
            {
                ["id"] = m.Id.ToString(),
                ["repairRequestId"] = m.RepairRequestId.ToString(),
                ["mediaType"] = m.MediaType.ToString(),
                ["storagePath"] = m.StoragePath,
                ["mimeType"] = m.MimeType ?? string.Empty,
                ["sizeBytes"] = m.SizeBytes,
                ["uploadedAt"] = Timestamp.FromDateTime(m.UploadedAt.UtcDateTime)
            })
            .Cast<object>()
            .ToList();

        var docRef = db.Collection(FirestoreCollections.RepairRequests).Document(repair.Id.ToString());
        var data = new Dictionary<string, object>
        {
            ["serviceOrderId"] = repair.ServiceOrderId.ToString(),
            ["createdByUserId"] = repair.CreatedByUserId.ToString(),
            ["issueDescription"] = repair.IssueDescription,
            ["priceEstimateCents"] = repair.PriceEstimateCents,
            ["urgency"] = repair.Urgency.ToString(),
            ["createdAt"] = Timestamp.FromDateTime(repair.CreatedAt.UtcDateTime),
            ["updatedAt"] = Timestamp.FromDateTime(repair.UpdatedAt.UtcDateTime),
            ["media"] = mediaMaps
        };

        if (repair.CustomerDecision is { } decision)
        {
            data["customerDecision"] = decision.ToString();
        }

        if (repair.DecidedAt is { } decidedAt)
        {
            data["decidedAt"] = Timestamp.FromDateTime(decidedAt.UtcDateTime);
        }

        if (!string.IsNullOrEmpty(repair.DecisionNote))
        {
            data["decisionNote"] = repair.DecisionNote;
        }

        await docRef.SetAsync(data, cancellationToken: cancellationToken);
    }
}
